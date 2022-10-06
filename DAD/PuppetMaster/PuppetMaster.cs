using System.Diagnostics;
using System;


string[] text = File.ReadAllLines("configuration_sample.txt");
string bankPorts = "";
string paxosPorts = "";

foreach (string line in text[0..3])
{
    ProcessStartInfo paxosServer = new ProcessStartInfo("C:/Users/Asus/source/repos/Dad/DAD/PaxosServer/bin/Debug/net6.0/PaxosServer.exe");
    paxosServer.Arguments = line.Split(':')[2];
    paxosServer.CreateNoWindow = false;
    paxosServer.UseShellExecute = true;
    Process p = Process.Start(paxosServer);
}
foreach (string line in text[3..6])
{
    ProcessStartInfo bankServer = new ProcessStartInfo("C:/Users/Asus/source/repos/Dad/DAD/BankServer/bin/Debug/net6.0/BankServer.exe");
    bankServer.Arguments = line.Split(':')[2];
    bankPorts += line.Split(':')[2] + "|";
    bankServer.CreateNoWindow = false;
    bankServer.UseShellExecute = true;
    Process p = Process.Start(bankServer);
}


foreach (string line in text[6..8])
{
    ProcessStartInfo client = new ProcessStartInfo("C:/Users/Asus/source/repos/Dad/DAD/Client/bin/Debug/net6.0/Client.exe");
    client.Arguments = bankPorts;
    Console.WriteLine(bankPorts);
    client.CreateNoWindow = false;
    client.UseShellExecute = true;
    Process p = Process.Start(client);
}   

