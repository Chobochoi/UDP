﻿using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace KWS
{
    internal class BuoyancyPass : WaterPass
    {
        internal override string PassName => "Water.BuoyancyPass";

        ComputeShader _computeShader;

        private CommandBuffer _cmd;

        public static ComputeBuffer Buffer;
        public static AsyncTextureSynchronizer<SurfaceData> AsyncTextureSynchronizer = new AsyncTextureSynchronizer<SurfaceData>();

        static List<IWaterSurfaceRequest>            _newRequests       = new List<IWaterSurfaceRequest>();
        static Dictionary<IWaterSurfaceRequest, int> _asyncLateRequests = new Dictionary<IWaterSurfaceRequest, int>();
        static NativeArray<SurfaceData>              _surfaceData;
        static NativeArray<SurfaceData>              JobsData;

        public BuoyancyPass()
        {
            AsyncTextureSynchronizer.DataUpdated = DataUpdated;
        }

        public override void Release()
        {
            Buffer?.Release();
            Buffer = null;

            AsyncTextureSynchronizer.ReleaseATSResources();

            if (_surfaceData.IsCreated) _surfaceData.Dispose();

            _asyncLateRequests.Clear();
            _newRequests.Clear();

            KW_Extensions.SafeDestroy(_computeShader);
            _computeShader = null;

            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.Release);
        }

        public override void ExecutePerFrame(HashSet<Camera> cameras, CustomFixedUpdates fixedUpdates)
        {
            UpdateBuoyancy();
        }
        public override void ExecuteBeforeCameraRendering(Camera cam, ScriptableRenderContext context)
        {
            //UpdateBuoyancy();

        }

        private void UpdateBuoyancy()
        {
#if UNITY_EDITOR
            if (KWS_CoreUtils.IsFrameDebuggerEnabled()) return;
#endif

            // if (!Application.isPlaying) return;


            try
            {
                if (_cmd == null) _cmd = new CommandBuffer() { name = PassName };
                _cmd.Clear();
                ExecuteInstance(_cmd);
                Graphics.ExecuteCommandBuffer(_cmd);
            }
            catch (ArgumentException e)
            {
                Release();
                Debug.LogError(e.Message);
            }
        }

        void ExecuteInstance(CommandBuffer cmd)
        {
            if (_computeShader == null) _computeShader = KWS_CoreUtils.LoadComputeShader("Common/CommandPass/KWS_BuoyancyPass_HeightReaback");
            if (_computeShader == null)
            {
                Debug.LogError("Can't initialite water buoyancy compute shader");
                return;
            }

            if (_newRequests.Count == 0 && WaterSurfaceJobs.EnableWaterJobSystem == false) return;
            if (AsyncTextureSynchronizer.IsBusy()) return;

            
            var actualLength = 0;
            if (WaterSurfaceJobs.EnableWaterJobSystem)
            {
                JobsData = WaterSurfaceJobs.DataToSend;
                
                if (JobsData.IsCreated && JobsData.Length == 0) return;
                actualLength = JobsData.Length;
                
                Buffer = KWS_CoreUtils.GetOrUpdateBuffer<SurfaceData>(ref Buffer, JobsData.Length);
                cmd.SetBufferData(Buffer, JobsData);
            }
            else
            {
               
                foreach (var request in _newRequests)
                {
                    actualLength += request.CurrentCount;
                }

                _surfaceData = KWS_CoreUtils.GetOrUpdateNativeArray(ref _surfaceData, actualLength, Allocator.Persistent);

                var nativeArrayIdx = 0;
                foreach (var request in _newRequests)
                {
                    request.SetAsyncData(_surfaceData, ref nativeArrayIdx);
                    _asyncLateRequests.Add(request, request.CurrentCount);
                }
                _newRequests.Clear();

                Buffer = KWS_CoreUtils.GetOrUpdateBuffer<SurfaceData>(ref Buffer, _surfaceData.Length);
                cmd.SetBufferData(Buffer, _surfaceData);
            }

        
         

            cmd.SetComputeTextureParam(_computeShader, 0, KWS_ShaderConstants.FFT.KWS_FftWavesDisplace, WaterSharedResources.FftWavesDisplacement.GetSafeArrayTexture());
            cmd.SetComputeTextureParam(_computeShader, 0, KWS_ShaderConstants.FFT.KWS_FftWavesNormal,   WaterSharedResources.FftWavesNormal.GetSafeArrayTexture());
            
            DynamicWavesPass.SetDefaultComputeShaderTextures(cmd, _computeShader, 0);
            foreach (var zone in KWS_TileZoneManager.VisibleDynamicWavesZones)
            {
                 DynamicWavesPass.UpdateShaderTexturesByID(zone.ID, (KWS_DynamicWavesSimulationZone)zone, cmd, _computeShader, 0);
            }

            if (KWS_TileZoneManager.MovableZone != null)
            {
                DynamicWavesPass.UpdateMovableShaderTexturesByID(((KWS_TileZoneManager.IWaterZone)KWS_TileZoneManager.MovableZone).ID, KWS_TileZoneManager.MovableZone, cmd, _computeShader, 0);
            }

            cmd.SetComputeBufferParam(_computeShader, 0, "SurfaceDataBuffer", Buffer);
            cmd.SetComputeIntParam(_computeShader, "KWS_SurfaceDataBufferCount", actualLength);
            cmd.DispatchCompute(_computeShader, 0, Mathf.CeilToInt(actualLength / 64f), 1, 1);

            AsyncTextureSynchronizer.EnqueueRequest(cmd, Buffer);

            //WaterSharedResources.FftGpuHeightAsyncData = AsyncTextureSynchronizer;
        }

        private void DataUpdated()
        {
            NativeArray<SurfaceData> bufferResult = AsyncTextureSynchronizer.CurrentBuffer();

            if (WaterSurfaceJobs.EnableWaterJobSystem)
            {
                WaterSurfaceJobs.Result = bufferResult;
            }
            else
            {
                var nativeArrayIdx = 0;
                foreach (var request in _asyncLateRequests)
                {
                    if (request.Key == null)
                    {
                        this.WaterLog("async request is null, elements: " + request.Value);
                        nativeArrayIdx += request.Value;
                        continue;
                    }
                    request.Key.GetAsyncData(bufferResult, ref nativeArrayIdx);
                }
                _asyncLateRequests.Clear();
            }
           
        }

        public static void TryGetWaterSurfaceData(IWaterSurfaceRequest surfaceRequest)
        {
            if (Time.frameCount <= 3) return; 
            //if (surfaceRequest.Result.Count == 0)
            //{
            //    Debug.LogError("You must use WaterSurfaceRequest.SetPositions before using TryGetWaterSurfaceData");
            //}

            if (!_newRequests.Contains(surfaceRequest))
            {
                _newRequests.Add(surfaceRequest);
            }
        }

    }


    public struct SurfaceData
    {
        public Vector3 Position;
        public float    Foam;
        public  Vector3 Velocity;
    };

    public static class WaterSurfaceJobs
    {
        public static NativeArray<SurfaceData> DataToSend;
        public static NativeArray<SurfaceData> Result;
        public static bool                     EnableWaterJobSystem;
    }

    public interface IWaterSurfaceRequest
    {
        public bool IsDataReady { get; }
        internal int CurrentCount { get; }
        internal void SetAsyncData(NativeArray<SurfaceData> bufferResult, ref int bufferIndex);
        internal void GetAsyncData(NativeArray<SurfaceData> bufferResult, ref int bufferIndex);
    }

    public class WaterSurfaceRequestList : IWaterSurfaceRequest
    {
        public List<SurfaceData> Result = new List<SurfaceData>();

        public bool IsDataReady { get; internal set; }
        int IWaterSurfaceRequest.CurrentCount => _currentCount;

        private int _currentCount;


        public void SetNewPositions(List<Vector3> positions)
        {
            if (positions.Count != Result.Count)
            {
                IsDataReady = false;
                Result.Clear();
                for (int i = 0; i < positions.Count; i++)
                {
                    var data = new SurfaceData();
                    data.Position = positions[i];
                    Result.Add(data);
                }
            }
            else
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    var data = Result[i];
                    data.Position.x = positions[i].x;
                    data.Position.z = positions[i].z;
                    Result[i]       = data;
                }
            }

            _currentCount = Result.Count;
        }


        void IWaterSurfaceRequest.SetAsyncData(NativeArray<SurfaceData> computeBuffer, ref int bufferIndex)
        {
            var count = Result.Count;
            for (int idx = 0; idx < count; idx++)
            {
                computeBuffer[bufferIndex] = Result[idx];
                bufferIndex++;
            }
        }

        void IWaterSurfaceRequest.GetAsyncData(NativeArray<SurfaceData> bufferResult, ref int bufferIndex)
        {
            IsDataReady = true;
            var count = Result.Count;
            for (int idx = 0; idx < count; idx++)
            {
                if (bufferIndex >= bufferResult.Length) break;

                var backupPos = Result[idx].Position;
                var newResult = bufferResult[bufferIndex]; //if the result will be ready after calling SetPositions, it can overwrite the current position. Save xz position
                newResult.Position.x = backupPos.x;
                newResult.Position.z = backupPos.z;
                Result[idx]          = newResult;

                bufferIndex++;
            }
        }
    }

    public class WaterSurfaceRequestArray : IWaterSurfaceRequest
    {
        public  bool          IsDataReady { get; internal set; }
        public  SurfaceData[] Result;
        private int           _currentCount;

        public void SetNewPositions(Vector3[] positions)
        {
            if (Result == null || positions.Length != Result.Length)
            {
                IsDataReady = false;
                Result      = new SurfaceData[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    var data = new SurfaceData();
                    data.Position = positions[i];
                    Result[i]     = data;
                }
            }
            else
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    var data = Result[i];
                    data.Position.x = positions[i].x;
                    data.Position.z = positions[i].z;
                    Result[i]       = data;
                }
            }

            _currentCount = Result.Length;
        }

        int IWaterSurfaceRequest.CurrentCount => _currentCount;

        void IWaterSurfaceRequest.SetAsyncData(NativeArray<SurfaceData> computeBuffer, ref int bufferIndex)
        {
            var count = Result.Length;
            for (int idx = 0; idx < count; idx++)
            {
                computeBuffer[bufferIndex] = Result[idx];
                bufferIndex++;
            }
        }

        void IWaterSurfaceRequest.GetAsyncData(NativeArray<SurfaceData> bufferResult, ref int bufferIndex)
        {
            IsDataReady = true;
            var count = Result.Length;
            for (int idx = 0; idx < count; idx++)
            {
                if (bufferIndex >= bufferResult.Length) break;

                var backupPos = Result[idx].Position;
                Result[idx]            = bufferResult[bufferIndex]; //if the result will be ready after calling SetPositions, it can overwrite the current position. Save xz position
                Result[idx].Position.x = backupPos.x;
                Result[idx].Position.z = backupPos.z;


                bufferIndex++;
            }
        }
    }

    public class WaterSurfaceRequestPoint : IWaterSurfaceRequest
    {
        public  bool        IsDataReady { get; internal set; }
        public  SurfaceData Result = new SurfaceData();
        private int         _currentCount;

        public void SetNewPosition(Vector3 position)
        {
            
            Result.Position.x = position.x;
            Result.Position.z = position.z;
            _currentCount     = 1;
        }

        int IWaterSurfaceRequest.CurrentCount => _currentCount;

        void IWaterSurfaceRequest.SetAsyncData(NativeArray<SurfaceData> computeBuffer, ref int bufferIndex)
        {
            computeBuffer[bufferIndex] = Result;
            bufferIndex++;

        }

        void IWaterSurfaceRequest.GetAsyncData(NativeArray<SurfaceData> bufferResult, ref int bufferIndex)
        {
            if (bufferIndex >= bufferResult.Length) return;

            IsDataReady = true;
            var backupPos = Result.Position;
            Result            = bufferResult[bufferIndex]; //if the result will be ready after calling SetPositions, it can overwrite the current position. Save xz position
            Result.Position.x = backupPos.x;
            Result.Position.z = backupPos.z;

            bufferIndex++;
        }
    }
}