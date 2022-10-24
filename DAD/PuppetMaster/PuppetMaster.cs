using System.Diagnostics;


string[] text = File.ReadAllLines("configuration_sample.txt");
List<string> bankPorts = new List<string>();
List<string> paxosPorts = new List<string>();
int numberClients = 0;

Dictionary<string, string> processes = new Dictionary<string, string>();

int iD = 1;

foreach (string line in text)
{
    if (line[0] == 'P')
    {
        if (line.Split(' ')[2].Equals("boney"))
        {
            paxosPorts.Add(line.Split(':')[2]);
        }
        else if (line.Split(' ')[2].Equals("bank"))
        {
            bankPorts.Add(line.Split(':')[2]);
        }
        else
        {
            numberClients++;
        }
    }
}

//inicializar os servidores paxos
for (int i = 0; i < paxosPorts.Count(); i++)
{
    List<string> sendingPorts = new List<string>();
    sendingPorts.Add(iD.ToString());
    iD++;
    sendingPorts.Add(paxosPorts[i]);
    foreach (string port in paxosPorts)
    {
        if (!port.Equals(paxosPorts[i]))
        {
            sendingPorts.Add(port);
        }
    }
    
    ProcessStartInfo paxosServer = new ProcessStartInfo("C:/Users/diogo/source/repos/Dad/DAD/PaxosServer/bin/Debug/net6.0/PaxosServer.exe");
    foreach (string port in sendingPorts)
    {
        paxosServer.ArgumentList.Add(port);
        
    }

    paxosServer.CreateNoWindow = false;
    paxosServer.UseShellExecute = true;
    Process p = Process.Start(paxosServer);
}


//inicializar os servidores do banco

for (int i = 0; i < bankPorts.Count(); i++)
{
    List<string> sendingPorts = new List<string>();
    sendingPorts.Add(iD.ToString());
    iD++;
    sendingPorts.Add(bankPorts[i]);
    foreach (string port in bankPorts)
    {
        if (!port.Equals(bankPorts[i]))
        {
            sendingPorts.Add(port);
        }
    }
    foreach (string port in paxosPorts)
    {
        sendingPorts.Add(port);
    }

    ProcessStartInfo bankServer = new ProcessStartInfo("C:/Users/diogo/source/repos/Dad/DAD/BankServer/bin/Debug/net6.0/BankServer.exe");

    foreach (string port in sendingPorts)
    {
        bankServer.ArgumentList.Add(port);
    }

    bankServer.CreateNoWindow = false;
    bankServer.UseShellExecute = true;
    Process p = Process.Start(bankServer);
}


//inicializar os clientes

for (int i = 0; i < numberClients; i++)
{
    ProcessStartInfo client = new ProcessStartInfo("C:/Users/diogo/source/repos/Dad/DAD/Client/bin/Debug/net6.0/Client.exe");
    client.ArgumentList.Add(iD.ToString());
    iD++;
    foreach (string port in bankPorts)
    {
        client.ArgumentList.Add(port);
    }
    client.CreateNoWindow = false;
    client.UseShellExecute = true;
    Process p = Process.Start(client);
}


