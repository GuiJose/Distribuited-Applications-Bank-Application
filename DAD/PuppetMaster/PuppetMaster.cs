using System.Diagnostics;


string[] text = File.ReadAllLines("configuration_sample.txt");
List<string> bankPorts = new List<string>();
List<string> paxosPorts = new List<string>();
int numberClients = 0;

Dictionary<string, string> processes = new Dictionary<string, string>();
List<string> configurationFiles = new List<string>();

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
            configurationFiles.Add(line.Split(' ')[3]);
        }
    }
}


string currentDir = Environment.CurrentDirectory;
string paxosDir = currentDir.Replace("PuppetMaster", "PaxosServer") + "/PaxosServer.exe";
string bankDir = currentDir.Replace("PuppetMaster", "BankServer") + "/BankServer.exe";
string clientDir = currentDir.Replace("PuppetMaster", "Client") + "/Client.exe";

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
    
    ProcessStartInfo paxosServer = new ProcessStartInfo(paxosDir);
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
    sendingPorts.Add(bankPorts.Count().ToString());
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

    ProcessStartInfo bankServer = new ProcessStartInfo(bankDir);

    foreach (string port in sendingPorts)
    {
        bankServer.ArgumentList.Add(port);
    }

    bankServer.CreateNoWindow = false;
    bankServer.UseShellExecute = true;
    Process p = Process.Start(bankServer);
}


//inicializar os clientes

int clientsRunning = 0;

for (int i = 0; i < numberClients; i++)
{
    ProcessStartInfo client = new ProcessStartInfo(clientDir);
    client.ArgumentList.Add(iD.ToString());
    client.ArgumentList.Add(configurationFiles[clientsRunning]);
    iD++;
    foreach (string port in bankPorts)
    {
        client.ArgumentList.Add(port);
    }
    client.CreateNoWindow = false;
    client.UseShellExecute = true;
    Process p = Process.Start(client);
    clientsRunning++;
}


