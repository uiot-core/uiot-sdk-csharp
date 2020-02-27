# uiot-sdk-csharp

用户可以直接基于visual studio 2018 使用 Solution M2Mqtt.sln。

# 文档说明

用户可以基于M2Mqtt.sln自行编译生成动态库`M2Mqtt.Net.dll`，也可以直接使用已经编译好的`./resource/lib-dll/M2Mqtt.Net.dll`动态库构建UCloud Client，通过MQTTS上报消息到UCloud 物联网平台，SSL加密版本使用TLSv1.2。

**【说明】**

本库在开源库的基础上加入了一下内容：

1. 增加了自动重连机制，自动订阅已经订阅的Topic；
2. 简化初始化流程，直接通过UCloud物联网平台的`ProductSN`，`DeviceSN`，`DeviceSecret`既可以接入平台；
3. 本库默认使用加密传输，协议版本为TLS1.2，使用自签名根证书`ca-cert.crt`，所以使用前需要在系统中安装该证书到受信任机构；



## 1. 导入TLS根证书

证书的文件目录：`./resource/ca-cert/ca-cert.crt`,安装该证书到系统`受信任的根证书颁发机构`。

**导入流程**
- 双击该文件`ca-cert.crt`；

- 依次单击：【安装证书】 - 【存储位置：本地计算机】 - 【下一步】 

- 继续单击：【是否允许应用对你的设备进行修改】 - 【是】

- 继续单击：【将所有的证书都放入下列存储 - 证书存储 - 浏览】 - 【受信任的根证书颁发机构】 - 【确定】- 【下一步】

- 继续单击：【完成】 - 【导入成功】

  

## 2. 接入流程

### 2.1 创建产品及设备

- 登录[UIoT-Core物联网控制台](https://console.ucloud.cn/uiot/),创建产品及设备，获取`产品序列号`、`设备序列号`、`设备密钥`。
- 参考文档：[创建产品](https://docs.ucloud.cn/iot/uiot-core/console_guide/product_device/create_products)，[创建设备](https://docs.ucloud.cn/iot/uiot-core/console_guide/product_device/create_devcies)

### 2.2 通过MqttClient接入物联网平台
```c#
// 1. 构造函数
/// <param name="region">UIoT-Core Region: cn-sh2</param>
/// <param name="productSN">UIoT-Core productSN</param>
/// <param name="deviceSN">UIoT-Core deviceSN</param>
/// <param name="deviceSecret">UIoT-Core deviceSecret</param>
/// <param name="handler">The Receive callback handler:void MqttMsgPublishEventHandler(object sender, MqttMsgPublishEventArgs e) </param>
        
UIoTClient uiotclient = new UIoTClient("cn-sh2", "ozuz63kum2i4djb3", "afnyhnizq9l4l9ev", "3ksk8dbg8ny3z3cf", callback_MqttMsgReceived);

// 2. 回调函数，处理下行消息
static void callback_MqttMsgReceived(object sender, MqttMsgPublishEventArgs e)
{
    // 接收到数据的处理逻辑，此处打印Topic和消息内容
    Console.WriteLine(e.Topic);
    Console.WriteLine(System.Text.Encoding.Default.GetString(e.Message));
}

// 3. 连接
uiotclient.Connect();

// 4. 订阅下行Topic，用于接收数据，可以在控制台Topic标签找到
uiotclient.Subscribe(new string[] { "/ozuz63kum2i4djb3/afnyhnizq9l4l9ev/downlink" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE});

// 5. 发送上行消息
uiotclient.Publish("/ozuz63kum2i4djb3/afnyhnizq9l4l9ev/uplink", System.Text.Encoding.UTF8.GetBytes("{\"test\":1}"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE);

```

### 2.3 查看上行消息

- 进入[UIoT-Core物联网控制台](https://console.ucloud.cn/uiot/),选择日志管理，通过设备行为、上行消息流、下行消息流等可以查看消息。
- 参考文档：[查看日志](https://docs.ucloud.cn/iot/uiot-core/console_guide/monitoring_maintenance/log)。



### 2.4 测试全部代码

```c#
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.UIoTClient;

namespace mqttconnect
{
    class Program
    {
        // 回调函数，处理下行消息
        static void callback_MqttMsgReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // 接收到数据的处理逻辑，此处打印Topic和消息内容
            Console.WriteLine(e.Topic);
            Console.WriteLine(System.Text.Encoding.Default.GetString(e.Message));
        }


        static void Main(string[] args)
        {
            Console.WriteLine("----Start!----");

            // 调用构造函数
            UIoTClient uiotclient = new UIoTClient("cn-sh2", "ozuz63kum2i4djb3", "afnyhnizq9l4l9ev", "3ksk8dbg8ny3z3cf", callback_MqttMsgReceived);
           
            //连接
            uiotclient.Connect();

            // 订阅下行Topic，下行Topic是指具有订阅权限的Topic
            uiotclient.Subscribe(new string[] { "/ozuz63kum2i4djb3/afnyhnizq9l4l9ev/downlink" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE});
      
            while (true)
            {
                uiotclient.Publish("/ozuz63kum2i4djb3/afnyhnizq9l4l9ev/uplink", System.Text.Encoding.UTF8.GetBytes("{\"test\":1}"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE);
                Thread.Sleep(1000);
            }
            
        }
    }
}
```
