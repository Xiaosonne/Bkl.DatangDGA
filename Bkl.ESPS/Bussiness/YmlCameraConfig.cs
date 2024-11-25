using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

public class YmlCameraConfig
{
    public string logLevel { get; set; }
    public string[] logDestinations { get; set; }
    public string logFile { get; set; }
    public string readTimeout { get; set; }
    public string writeTimeout { get; set; }
    [YamlMember(typeof(int))]
    public string readBufferCount { get; set; }
    public object externalAuthenticationURL { get; set; }
    [YamlMember(typeof(Boolean))]
    public string api { get; set; }
    public string apiAddress { get; set; }
    [YamlMember(typeof(Boolean))]
    public string metrics { get; set; }
    public string metricsAddress { get; set; }
    [YamlMember(typeof(Boolean))]
    public string pprof { get; set; }
    public string pprofAddress { get; set; }
    public object runOnConnect { get; set; }
    [YamlMember(typeof(Boolean))]
    public string runOnConnectRestart { get; set; }
    [YamlMember(typeof(Boolean))]
    public string rtspDisable { get; set; }
    public string[] protocols { get; set; }
    public string encryption { get; set; }
    public string rtspAddress { get; set; }
    public string rtspsAddress { get; set; }
    public string rtpAddress { get; set; }
    public string rtcpAddress { get; set; }
    public string multicastIPRange { get; set; }
    [YamlMember(typeof(int))]
    public string multicastRTPPort { get; set; }
    [YamlMember(typeof(int))]
    public string multicastRTCPPort { get; set; }
    public string serverKey { get; set; }
    public string serverCert { get; set; }
    public string[] authMethods { get; set; }
    [YamlMember(typeof(Boolean))]
    public string rtmpDisable { get; set; }
    public string rtmpAddress { get; set; }
    public string rtmpEncryption { get; set; }
    public string rtmpsAddress { get; set; }
    public string rtmpServerKey { get; set; }
    public string rtmpServerCert { get; set; }
    [YamlMember(typeof(Boolean))]
    public string hlsDisable { get; set; }
    public string hlsAddress { get; set; }
    [YamlMember(typeof(Boolean))]
    public string hlsAlwaysRemux { get; set; }

    public string hlsVariant { get; set; }
    [YamlMember(typeof(int))]
    public string hlsSegmentCount { get; set; }
    public string hlsSegmentDuration { get; set; }
    public string hlsPartDuration { get; set; }
    public string hlsSegmentMaxSize { get; set; }
    public string hlsAllowOrigin { get; set; }
    [YamlMember(typeof(Boolean))]
    public string hlsEncryption { get; set; }
    public string hlsServerKey { get; set; }
    public string hlsServerCert { get; set; }
    public object[] hlsTrustedProxies { get; set; }
    public Dictionary<string, YmlPath> paths { get; set; }
    public class YmlPath
    {
        public string source { get; set; }
        public string sourceProtocol { get; set; }
        public string sourceAnyPortEnable { get; set; }
        public object sourceFingerprint { get; set; }
        [YamlMember(typeof(Boolean))]
        public bool sourceOnDemand { get; set; }
        public string sourceOnDemandStartTimeout { get; set; }
        public string sourceOnDemandCloseAfter { get; set; }
        public object sourceRedirect { get; set; }
        public string disablePublisherOverride { get; set; }
        public object fallback { get; set; }
        public string rpiCameraCamID { get; set; }
        public string rpiCameraWidth { get; set; }
        public string rpiCameraHeight { get; set; }
        public string rpiCameraHFlip { get; set; }
        public string rpiCameraVFlip { get; set; }
        public string rpiCameraBrightness { get; set; }
        public string rpiCameraContrast { get; set; }
        public string rpiCameraSaturation { get; set; }
        public string rpiCameraSharpness { get; set; }
        public string rpiCameraExposure { get; set; }
        public string rpiCameraAWB { get; set; }
        public string rpiCameraDenoise { get; set; }
        public string rpiCameraShutter { get; set; }
        public string rpiCameraMetering { get; set; }
        public string rpiCameraGain { get; set; }
        public string rpiCameraEV { get; set; }
        public object rpiCameraROI { get; set; }
        public object rpiCameraTuningFile { get; set; }
        public object rpiCameraMode { get; set; }
        public string rpiCameraFPS { get; set; }
        public string rpiCameraIDRPeriod { get; set; }
        public string rpiCameraBitrate { get; set; }
        public string rpiCameraProfile { get; set; }
        public string rpiCameraLevel { get; set; }
        public object publishUser { get; set; }
        public object publishPass { get; set; }
        public object[] publishIPs { get; set; }
        public object readUser { get; set; }
        public object readPass { get; set; }
        public object[] readIPs { get; set; }
        public object runOnInit { get; set; }
        public string runOnInitRestart { get; set; }
        public object runOnDemand { get; set; }
        public string runOnDemandRestart { get; set; }
        public string runOnDemandStartTimeout { get; set; }
        public string runOnDemandCloseAfter { get; set; }
        public object runOnReady { get; set; }
        public string runOnReadyRestart { get; set; }
        public object runOnRead { get; set; }
        public string runOnReadRestart { get; set; }
    }
}
