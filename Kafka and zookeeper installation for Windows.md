# Installation instructions for Kafka/Zookeeper on Windows

1. Install Java JDK
1. Set env variable 'JAVA_HOME' to 'c:\Program Files\Java\jdk1.8.0_144'
1. Set env variable 'ZOOKEEPER_HOME' to 'C:\dev\Kafka\zookeeper-3.4.10\', and append 'C:\dev\Kafka\zookeeper-3.4.10\' to "Path" sys variable
1. Rename "zoo_sample.cfg" to "zoo.cfg" in C:\Tools\zookeeper-3.4.10\conf, and change its value for "dataDir" from "/tmp/zookeeper" to "c:\zookeeper-3.4.10\data" (or whatever)
1. Run "C:\dev\Kafka\zookeeper-3.4.10\bin\zkServer.cmd"
1. Open 'C:\dev\Kafka\kafka_2.11-0.11.0.0\config\server.properties' and set its value for "log.dirs" to "C:/dev/Kafka/kafka_2.11-0.11.0.0/kafka-logs"
1. Run "C:\dev\Kafka\kafka_2.11-0.11.0.0\bin\windows\kafka-server-start.bat C:\dev\Kafka\kafka_2.11-0.11.0.0\config\server.properties"
1. Use Kafka Tool (www.kafkatool.com/index.html) to connect to localhost:2181
1. Create topics/partitions/etc (https://www.pluralsight.com/courses/apache-kafka-getting-started if you're unsure about these)
1. Connect Sunergeo

For more info, see:
- Kafka installation: https://medium.com/@shaaslam/installing-apache-kafka-on-windows-495f6f2fd3c8
- Zookeeper installation: https://medium.com/@shaaslam/installing-apache-zookeeper-on-windows-45eda303e835
