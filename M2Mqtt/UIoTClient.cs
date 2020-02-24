using System;
using System.Collections.Generic;
#if !(WINDOWS_APP || WINDOWS_PHONE_APP)
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
#endif
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
#if (WINDOWS_APP || WINDOWS_PHONE_APP)
using Windows.Networking.Sockets;
#endif
using MqttUtility = uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt.UIoTClient
{
    /// <summary>
    /// UIoT Client
    /// </summary>
    public class UIoTClient
    {
        // variable header fields
        internal const ushort KEEP_ALIVE_PERIOD_DEFAULT = 120; // seconds

        private List<string> subtopics;
        private List<byte> subtopicqoss;

        public MqttClient mqttclient;
        // UCloud uiot-core productSN and deviceSN
        private string productSN;
        private string deviceSN;

        // UCloud uiot-core deviceSecret
        private string deviceSecret;

        // UCloud uiot-core region
        private string region;

        // private message received handler
        private MqttClient.MqttMsgPublishEventHandler umqttMsgPublishReceivedHandler;

        /// <summary>
        /// Connection status between client and broker
        /// </summary>
        public bool IsConnected { get; private set; }

        private bool disconnected = false;


        static void callback_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Console.WriteLine(e.Topic);
            Console.WriteLine(System.Text.Encoding.Default.GetString(e.Message));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="region">UIoT-Core Region: cn-sh2</param>
        /// <param name="productSN">UIoT-Core productSN</param>
        /// <param name="deviceSN">UIoT-Core deviceSN</param>
        /// <param name="deviceSecret">UIoT-Core deviceSecret</param>
        /// <param name="secure">UIoT-Core secure mode:1883/8883</param>
        public UIoTClient(string region, string productSN, string deviceSN, string deviceSecret, bool secure) :
            this(region, productSN, deviceSN, deviceSecret, secure, null, callback_MqttMsgPublishReceived)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="region">UIoT-Core Region: cn-sh2</param>
        /// <param name="productSN">UIoT-Core productSN</param>
        /// <param name="deviceSN">UIoT-Core deviceSN</param>
        /// <param name="deviceSecret">UIoT-Core deviceSecret</param>
        /// <param name="handler">The Receive callback handler:void MqttMsgPublishEventHandler(object sender, MqttMsgPublishEventArgs e) </param>
        public UIoTClient(string region, string productSN, string deviceSN, string deviceSecret, MqttClient.MqttMsgPublishEventHandler handler) :
            this(region, productSN, deviceSN, deviceSecret, true, null, handler)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="region">UIoT-Core Region: cn-sh2</param>
        /// <param name="productSN">UIoT-Core productSN</param>
        /// <param name="deviceSN">UIoT-Core deviceSN</param>
        /// <param name="deviceSecret">UIoT-Core deviceSecret</param>
        /// <param name="caCertFilename">CA certificate filepath for secure connection</param>
        /// <param name="handler">The Receive callback handler:void MqttMsgPublishEventHandler(object sender, MqttMsgPublishEventArgs e) </param>
        public UIoTClient(string region, string productSN, string deviceSN, string deviceSecret, string caCertFilename, MqttClient.MqttMsgPublishEventHandler handler) :
            this(region,productSN,deviceSN,deviceSecret,true, caCertFilename, handler)
        { 
        }
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="region">UIoT-Core Region: cn-sh2</param>
            /// <param name="productSN">UIoT-Core productSN</param>
            /// <param name="deviceSN">UIoT-Core deviceSN</param>
            /// <param name="deviceSecret">UIoT-Core deviceSecret</param>
            /// <param name="secure">UIoT-Core secure mode:1883/8883</param>
            /// <param name="caCertFilename">CA certificate filepath for secure connection</param>
            /// <param name="handler">The Receive callback handler:void MqttMsgPublishEventHandler(object sender, MqttMsgPublishEventArgs e) </param>
            public UIoTClient(string region, string productSN, string deviceSN, string deviceSecret, bool secure, string caCertFilename, MqttClient.MqttMsgPublishEventHandler handler)
            {
#if !(WINDOWS_APP || WINDOWS_PHONE_APP)
            X509Certificate caCert;
            if (caCertFilename == null)
                caCert = null;
            else
                caCert = X509Certificate.CreateFromCertFile(caCertFilename);

            mqttclient = new MqttClient("mqtt-" + region + ".iot.ucloud.cn", secure?8883:1883, secure, caCert, null, MqttSslProtocols.TLSv1_2);
#else
            mqttclient = new MqttClient("mqtt-" + region + "iot.ucloud.cn", MqttSettings.MQTT_BROKER_DEFAULT_SSL_PORT, true, MqttSslProtocols.TLSv1_2);
#endif
            this.productSN = productSN;
            this.deviceSN = deviceSN;
            this.deviceSecret = deviceSecret;
            this.region = region;
            subtopics = new List<string>();
            subtopicqoss = new List<byte>();
            this.IsConnected = false;

            this.umqttMsgPublishReceivedHandler = handler;
            mqttclient.MqttMsgPublishReceived += handler;
            // 连接断开事件绑定
            mqttclient.ConnectionClosed += (sender, e) =>
            {
                this.IsConnected = false;
                this.Connect();
            };
        }

        /// <summary>
        /// connect to the mqtt broker one time;
        /// </summary>
        private void ConnectOnce()
        {
            try
            {
                this.mqttclient.Connect(this.productSN + "." + this.deviceSN, this.productSN + "|" + this.deviceSN + "|1", deviceSecret, false, MqttMsgConnect.QOS_LEVEL_AT_MOST_ONCE, false, null, null, true, KEEP_ALIVE_PERIOD_DEFAULT);
            }
            catch (Exception e)
            {
#if TRACE
                MqttUtility.Trace.WriteLine(TraceLevel.Error, "reconnect exception: {0}", e.ToString());
#endif
            }

            if (this.mqttclient.IsConnected)
            {
                if (this.subtopics.Count != 0)
                {
                    this.mqttclient.Subscribe(subtopics.ToArray(), subtopicqoss.ToArray());
                }
                this.IsConnected = true;
                this.disconnected = false;
            }
        }
#if (WINDOWS_APP || WINDOWS_PHONE_APP)
        private async void WaitForSeconds(uint millisecondsTimeout)
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(millisecondsTimeout));
            // do something after millisecondsTimeout seconds!
        }
#endif

        /// <summary>
        /// Thread for handling keep alive message
        /// </summary>
        private void ReConnect()
        {
            while (!this.IsConnected)
            {
                if (this.disconnected)
                    break;

                this.ConnectOnce();

#if !(WINDOWS_APP || WINDOWS_PHONE_APP)
                if(!this.IsConnected)
                    Thread.Sleep(2000);
#else
                if(!this.IsConnected)
                    this.WaitForSeconds(2000);
#endif
            }
        }

        /// <summary>
        /// auto reconnect to the broker when mqtt disconnected
        /// </summary>
        /// <param name="topics">List of topics to subscribe</param>
        /// <param name="qosLevels">QOS levels related to topics</param>
        /// <returns>Message Id related to SUBSCRIBE message</returns>
        public void Connect()
        {
            Fx.StartThread(this.ReConnect);
        }

        /// <summary>
        /// Disconnect from broker
        /// </summary>
        public void Disconnect()
        {
            this.disconnected = true;
            try
            {
                this.mqttclient.Disconnect();
            }
            catch (Exception e)
            {
#if TRACE
                MqttUtility.Trace.WriteLine(TraceLevel.Error, "diconnected: {0}", e.ToString());
#endif
            }
        }

        /// <summary>
        /// Subscribe for message topics
        /// </summary>
        /// <param name="topics">List of topics to subscribe</param>
        /// <param name="qosLevels">QOS levels related to topics</param>
        /// <returns>Message Id related to SUBSCRIBE message</returns>
        public ushort Subscribe(string[] topics, byte[] qosLevels)
        {
            this.subtopics.AddRange(topics);
            this.subtopicqoss.AddRange(qosLevels);
            return mqttclient.Subscribe(topics, qosLevels);
        }

        /// <summary>
        /// Unsubscribe for message topics
        /// </summary>
        /// <param name="topics">List of topics to unsubscribe</param>
        /// <returns>Message Id in UNSUBACK message from broker</returns>
        public ushort Unsubscribe(string[] topics)
        {
            foreach (var topic in topics)
            {
                //this.subtopics.FindAll(j => j.Contains(topic));
                int ret = 0;
                while (true)
                {
                    ret = this.subtopics.FindIndex(j => j.Contains(topic));
                    if (ret != -1)
                    {
                        this.subtopics.RemoveAt(ret);
                        this.subtopicqoss.RemoveAt(ret);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return mqttclient.Unsubscribe(topics);
        }

        /// <summary>
        /// Publish a message asynchronously (QoS Level 0 and not retained)
        /// </summary>
        /// <param name="topic">Message topic</param>
        /// <param name="message">Message data (payload)</param>
        /// <returns>Message Id related to PUBLISH message</returns>
        public ushort Publish(string topic, byte[] message, byte qosLevel)
        {
            return this.mqttclient.Publish(topic, message,qosLevel,false);
        }
    }
}
