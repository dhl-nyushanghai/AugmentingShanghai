/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System;
using System.Threading.Tasks;
using Immersal.AR;
using Immersal.REST;
using Rokid.UXR.Module;
using Rokid.UXR.Native;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Immersal.XR.Rokid
{
    public struct RokidCameraData
    {
        public long Timestamp;
        public int Width;
        public int Height;
        public byte[] Bytes;
        public Pose Pose;
        public Vector4 Distortion;
        public float Alpha;
        public Vector4 Intrinsics;
    }

    public enum CameraType
    {
        NV21 = 2 //ARGB not supported.
    }

    public class RokidLocalizer : LocalizerBase
    {
        private static RokidLocalizer instance = null;

        public static RokidLocalizer Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = UnityEngine.Object.FindObjectOfType<RokidLocalizer>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No RokidLocalizer instance found. Ensure one exists in the scene.");
                }

                return instance;
            }
        }

        public int Channels => m_cameraType == CameraType.NV21 ? 1 : 4;
        public int MultipleLocalizationsCount = 20;
        public UnityEvent OnMultipleLocalizations = null;
        public RokidCameraData m_LatestCameraData;

        private bool m_IsInitialized = false;
        private bool m_MultipleLocalizationEventInvoked = false;
        private CameraType m_cameraType = CameraType.NV21;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            if (instance != this)
            {
                Debug.LogError("There must be only one RokidLocalizer object in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }
        }

        public void Init()
        {
            NativeInterface.NativeAPI.StartCameraPreview();
            // 1 = ARGB, 2 = NV21
            NativeInterface.NativeAPI.SetCameraPreviewDataType((int) m_cameraType);
            NativeInterface.NativeAPI.OnCameraDataUpdate += OnCameraDataUpdate;
            m_IsInitialized = true;
            isTracking = true;
        }

        public override void Start()
        {
            base.Start();
            m_Sdk.RegisterLocalizer(instance);

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            NativeInterface.NativeAPI.Recenter();
        }

        /// <summary>
        /// Listener of Camera data
        /// </summary>
        /// <param name="width">preview size width</param>
        /// <param name="height">preview size height</param>
        /// <param name="yuvImage">camera image</param>
        /// <param name="ts">timestamp</param>
        public void OnCameraDataUpdate(int width, int height, byte[] data, long ts)
        {
            Pose pose = NativeInterface.NativeAPI.GetHistoryCameraPhysicsPose(ts);
            
            // fisheye:alpha,k1,k2,k3,k4;
            var d = new float[5];
            NativeInterface.NativeAPI.GetDistortion(d);
            var distortionCoefficients = new Vector4(d[1], d[2], d[3], d[4]);
            var alpha = d[0];
            var intrinsics = GetIntrinsics();

            m_LatestCameraData = new RokidCameraData
            {
                Timestamp = ts,
                Width = width,
                Height = height,
                Bytes = data,
                Pose = pose,
                Distortion = distortionCoefficients,
                Intrinsics = intrinsics,
                Alpha = alpha
            };
        }

        public void Release()
        {
            if (m_IsInitialized)
            {
                NativeInterface.NativeAPI.OnCameraDataUpdate -= OnCameraDataUpdate;
                NativeInterface.NativeAPI.StopCameraPreview();
                NativeInterface.NativeAPI.ClearCameraDataUpdate();
                m_IsInitialized = false;
                isTracking = false;
            }
        }

        override public void OnDestroy()
        {
            Release();
            base.OnDestroy();
        }

        override public void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Release();
            }

            base.OnApplicationPause(pauseStatus);
        }

        override protected void Update()
        {
            if (m_IsInitialized == false && NativeInterface.NativeAPI.IsPreviewing())
            {
                Init();
            }

            base.Update();
        }

        public override async void Localize()
        {
            var data = m_LatestCameraData;
            data = await UndistortImage(data);
            SetPixelBuffer(data.Bytes);

            if (m_PixelBuffer != IntPtr.Zero)
            {
                stats.localizationAttemptCount++;

                Vector3 camPos = data.Pose.position;
                Quaternion camRot = data.Pose.rotation;
                float startTime = Time.realtimeSinceStartup;
                
                float[] rotArray = new float[4];
                if (SolverType == SolverType.Lean)
                {
                    Quaternion qRot = camRot;
                    ARHelper.GetRotation(ref qRot);
                    qRot = ARHelper.SwitchHandedness(qRot);
                    rotArray = new float[4] { qRot.x, qRot.y, qRot.z, qRot.w };
                }

                Task<LocalizeInfo> t = Task.Run(() =>
                {
                    if (SolverType == SolverType.Lean)
                        return Immersal.Core.LocalizeImage(data.Width, data.Height, ref data.Intrinsics, m_PixelBuffer, rotArray);

                    return Immersal.Core.LocalizeImage(data.Width, data.Height, ref data.Intrinsics, m_PixelBuffer);
                });

                await t;

                LocalizeInfo locInfo = t.Result;

                Matrix4x4 resultMatrix = Matrix4x4.identity;
                resultMatrix.m00 = locInfo.r00;
                resultMatrix.m01 = locInfo.r01;
                resultMatrix.m02 = locInfo.r02;
                resultMatrix.m03 = locInfo.px;
                resultMatrix.m10 = locInfo.r10;
                resultMatrix.m11 = locInfo.r11;
                resultMatrix.m12 = locInfo.r12;
                resultMatrix.m13 = locInfo.py;
                resultMatrix.m20 = locInfo.r20;
                resultMatrix.m21 = locInfo.r21;
                resultMatrix.m22 = locInfo.r22;
                resultMatrix.m23 = locInfo.pz;

                Vector3 pos = resultMatrix.GetColumn(3);
                Quaternion rot = resultMatrix.rotation;

                int mapHandle = locInfo.handle;
                int mapId = ARMap.MapHandleToId(mapHandle);
                float elapsedTime = Time.realtimeSinceStartup - startTime;

                if (mapId > 0 && ARSpace.mapIdToMap.ContainsKey(mapId))
                {
                    LocalizerDebugLog(string.Format("Relocalized in {0} seconds", elapsedTime));
                    stats.localizationSuccessCount++;

                    if (stats.localizationSuccessCount >= MultipleLocalizationsCount &&
                        !m_MultipleLocalizationEventInvoked)
                    {
                        OnMultipleLocalizations.Invoke();
                        m_MultipleLocalizationEventInvoked = true;
                    }

                    ARMap map = ARSpace.mapIdToMap[mapId];

                    if (mapId != lastLocalizedMapId)
                    {
                        if (resetOnMapChange)
                        {
                            Reset();
                        }

                        lastLocalizedMapId = mapId;
                        OnMapChanged?.Invoke(mapId);
                    }

                    rot *= Quaternion.Euler(0f, 0f, 180.0f);
                    pos = ARHelper.SwitchHandedness(pos);
                    rot = ARHelper.SwitchHandedness(rot);

                    MapOffset mo = ARSpace.mapIdToOffset[mapId];

                    Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
                    Vector3 scaledPos = Vector3.Scale(pos, mo.scale);
                    Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
                    Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
                    Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

                    if (useFiltering)
                        mo.space.filter.RefinePose(m);
                    else
                        ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

                    Vector3 p = m.GetColumn(3);
                    Vector3 euler = m.rotation.eulerAngles;

                    GetLocalizerPose(out lastLocalizedPose, mapId, pos, rot, m.inverse);
                    map.NotifySuccessfulLocalization(mapId);
                    OnPoseFound?.Invoke(lastLocalizedPose);
                }
                else
                {
                    LocalizerDebugLog(string.Format("Localization attempt failed after {0} seconds", elapsedTime));
                }
            }
            else
            {
                Debug.LogError("No camera pixel buffer");
            }

            base.Localize();
        }
        
        public override async void LocalizeServer(SDKMapId[] mapIds)
        {
            if (m_LatestCameraData.Bytes != null)
            {
                stats.localizationAttemptCount++;

                var data = m_LatestCameraData;
                data = await UndistortImage(data);

                JobLocalizeServerAsync j = new JobLocalizeServerAsync();

                Vector3 camPos = data.Pose.position;
                Quaternion camRot = data.Pose.rotation;
                float startTime = Time.realtimeSinceStartup;

                Task<(byte[], CaptureInfo)> t = Task.Run(() =>
                {
                    byte[] capture = new byte[Channels * data.Width * data.Height + 8192];
                    CaptureInfo info =
                        Immersal.Core.CaptureImage(capture, capture.Length, data.Bytes,
                            data.Width, data.Height, Channels);
                    Array.Resize(ref capture, info.captureSize);
                    return (capture, info);
                });

                await t;

                j.image = t.Result.Item1;
                j.intrinsics = data.Intrinsics;
                j.mapIds = mapIds;
                
                j.solverType = (int)SolverType;
                float[] rotArray = new float[4];
                if (SolverType == SolverType.Lean)
                {
                    Quaternion qRot = camRot;
                    ARHelper.GetRotation(ref qRot);
                    qRot = ARHelper.SwitchHandedness(qRot);
                    rotArray = new float[4] { qRot.x, qRot.y, qRot.z, qRot.w };
                }
                j.camRot = rotArray;

                j.OnResult += (SDKLocalizeResult result) =>
                {
                    float elapsedTime = Time.realtimeSinceStartup - startTime;

                    if (result.success)
                    {
                        LocalizerDebugLog(
                            "*************************** On-Server Localization Succeeded ***************************");
                        LocalizerDebugLog(string.Format("Relocalized in {0} seconds", elapsedTime));

                        int mapId = result.map;

                        if (mapId > 0 && ARSpace.mapIdToMap.ContainsKey(mapId))
                        {
                            ARMap map = ARSpace.mapIdToMap[mapId];

                            if (mapId != lastLocalizedMapId)
                            {
                                if (resetOnMapChange)
                                {
                                    Reset();
                                }

                                lastLocalizedMapId = mapId;
                                OnMapChanged?.Invoke(mapId);
                            }

                            MapOffset mo = ARSpace.mapIdToOffset[mapId];
                            stats.localizationSuccessCount++;
                            
                            if (stats.localizationSuccessCount >= MultipleLocalizationsCount &&
                                !m_MultipleLocalizationEventInvoked)
                            {
                                OnMultipleLocalizations.Invoke();
                                m_MultipleLocalizationEventInvoked = true;
                            }

                            Matrix4x4 responseMatrix = Matrix4x4.identity;
                            responseMatrix.m00 = result.r00;
                            responseMatrix.m01 = result.r01;
                            responseMatrix.m02 = result.r02;
                            responseMatrix.m03 = result.px;
                            responseMatrix.m10 = result.r10;
                            responseMatrix.m11 = result.r11;
                            responseMatrix.m12 = result.r12;
                            responseMatrix.m13 = result.py;
                            responseMatrix.m20 = result.r20;
                            responseMatrix.m21 = result.r21;
                            responseMatrix.m22 = result.r22;
                            responseMatrix.m23 = result.pz;

                            Vector3 pos = responseMatrix.GetColumn(3);
                            Quaternion rot = responseMatrix.rotation;

                            rot *= Quaternion.Euler(0f, 0f, 180.0f);
                            pos = ARHelper.SwitchHandedness(pos);
                            rot = ARHelper.SwitchHandedness(rot);

                            Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
                            Vector3 scaledPos = Vector3.Scale(pos, mo.scale);
                            Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
                            Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
                            Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

                            if (useFiltering)
                                mo.space.filter.RefinePose(m);
                            else
                                ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

                            double[] ecef = map.MapToEcefGet();
                            LocalizerBase.GetLocalizerPose(out lastLocalizedPose, mapId, pos, rot, m.inverse, ecef);
                            map.NotifySuccessfulLocalization(mapId);
                            OnPoseFound?.Invoke(lastLocalizedPose);
                        }
                    }
                    else
                    {
                        LocalizerDebugLog(
                            "*************************** On-Server Localization Failed ***************************");
                        LocalizerDebugLog(string.Format("Localization attempt failed after {0} seconds", elapsedTime));
                    }
                };

                await j.RunJobAsync();
            }
            else
            {
                Debug.LogError("No camera pixel buffer");
            }

            base.LocalizeServer(mapIds);
        }

        public override async void LocalizeGeoPose(SDKMapId[] mapIds)
        {
            //NOT SUPPORTED IN BACKEND. KNOWN ISSUE AS OF 2nd FEB 2024.
            if (m_LatestCameraData.Bytes != null)
            {
                stats.localizationAttemptCount++;

                var data = m_LatestCameraData;
                data = await UndistortImage(data);

                JobGeoPoseAsync j = new JobGeoPoseAsync();

                Vector3 camPos = data.Pose.position;
                Quaternion camRot = data.Pose.rotation;
                float startTime = Time.realtimeSinceStartup;

                Task<(byte[], CaptureInfo)> t = Task.Run(() =>
                {
                    byte[] capture = new byte[Channels * data.Width * data.Height + 8192];
                    CaptureInfo info =
                        Immersal.Core.CaptureImage(capture, capture.Length, data.Bytes,
                            data.Width, data.Height, Channels);
                    Array.Resize(ref capture, info.captureSize);
                    return (capture, info);
                });

                await t;

                j.image = t.Result.Item1;
                j.intrinsics = data.Intrinsics;
                j.mapIds = mapIds;
                
                j.solverType = (int)SolverType;
                float[] rotArray = new float[4];
                if (SolverType == SolverType.Lean)
                {
                    Quaternion qRot = camRot;
                    ARHelper.GetRotation(ref qRot);
                    qRot = ARHelper.SwitchHandedness(qRot);
                    rotArray = new float[4] { qRot.x, qRot.y, qRot.z, qRot.w };
                }
                j.camRot = rotArray;

                j.OnResult += (SDKGeoPoseResult result) =>
                {
                    float elapsedTime = Time.realtimeSinceStartup - startTime;

                    if (result.success)
                    {
                        LocalizerDebugLog(
                            "*************************** GeoPose Localization Succeeded ***************************");
                        LocalizerDebugLog(string.Format("Relocalized in {0} seconds", elapsedTime));

                        int mapId = result.map;
                        double latitude = result.latitude;
                        double longitude = result.longitude;
                        double ellipsoidHeight = result.ellipsoidHeight;
                        Quaternion rot = new Quaternion(result.quaternion[1], result.quaternion[2],
                            result.quaternion[3], result.quaternion[0]);
                        LocalizerDebugLog(string.Format(
                            "GeoPose returned latitude: {0}, longitude: {1}, ellipsoidHeight: {2}, quaternion: {3}",
                            latitude, longitude, ellipsoidHeight, rot));

                        double[] ecef = new double[3];
                        double[] wgs84 = new double[3] {latitude, longitude, ellipsoidHeight};
                        Core.PosWgs84ToEcef(ecef, wgs84);
                        if (ARSpace.mapIdToMap.ContainsKey(mapId))
                        {
                            ARMap map = ARSpace.mapIdToMap[mapId];
                            if (mapId != lastLocalizedMapId)
                            {
                                if (resetOnMapChange)
                                {
                                    Reset();
                                }

                                lastLocalizedMapId = mapId;
                                OnMapChanged?.Invoke(mapId);
                            }

                            MapOffset mo = ARSpace.mapIdToOffset[mapId];
                            stats.localizationSuccessCount++;
                            double[] mapToEcef = map.MapToEcefGet();
                            Vector3 mapPos;
                            Quaternion mapRot;
                            Core.PosEcefToMap(out mapPos, ecef, mapToEcef);
                            Core.RotEcefToMap(out mapRot, rot, mapToEcef);
                            ARHelper.GetRotation(ref mapRot);
                            mapPos = ARHelper.SwitchHandedness(mapPos);
                            mapRot = ARHelper.SwitchHandedness(mapRot);

                            Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
                            Vector3 scaledPos = Vector3.Scale(mapPos, mo.scale);
                            Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, mapRot, Vector3.one);
                            Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
                            Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

                            if (useFiltering)
                                mo.space.filter.RefinePose(m);
                            else
                                ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

                            LocalizerBase.GetLocalizerPose(out lastLocalizedPose, mapId, cloudSpace.GetColumn(3),
                                cloudSpace.rotation, m.inverse, mapToEcef);
                            map.NotifySuccessfulLocalization(mapId);
                            OnPoseFound?.Invoke(lastLocalizedPose);
                        }
                    }
                    else
                    {
                        LocalizerDebugLog(
                            "*************************** GeoPose Localization Failed ***************************");
                        LocalizerDebugLog(string.Format("GeoPose localization attempt failed after {0} seconds",
                            elapsedTime));
                    }
                };

                await j.RunJobAsync();
            }
            else
            {
                Debug.LogError("No camera pixel buffer");
            }

            base.LocalizeGeoPose(mapIds);
        }

        private void SetPixelBuffer(byte[] data)
        {
            unsafe
            {
                fixed (byte* pinnedData = data)
                {
                    m_PixelBuffer = (IntPtr) pinnedData;
                }
            }
        }

        private Vector4 GetIntrinsics()
        {
            Vector4 intrinsics = Vector4.zero;
            float[] focalLength = new float[2];
            float[] principalPoint = new float[2];
            NativeInterface.NativeAPI.GetFocalLength(focalLength);
            NativeInterface.NativeAPI.GetPrincipalPoint(principalPoint);
            intrinsics.x = focalLength[0];
            intrinsics.y = focalLength[1];
            intrinsics.z = principalPoint[0];
            intrinsics.w = principalPoint[1];
            return intrinsics;
        }
        
        private async Task<RokidCameraData> UndistortImage(RokidCameraData data)
        {
            Task<(byte[], int)> d = Task.Run(() =>
            {
                var result = new byte[Channels * data.Width * data.Height];
                var r = Immersal.Core.UndistortFishEyeImage(result, result.Length, data.Bytes,
                    data.Width, data.Height, Channels, ref data.Intrinsics,
                    ref data.Distortion, data.Alpha);
                return (result, r);
            });

            await d;
            data.Bytes = d.Result.Item1;
            return data;
        }
    }
}