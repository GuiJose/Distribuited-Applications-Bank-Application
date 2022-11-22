# Dad
DAD - Bank Aplication
Project developed in C#. Our goal was to build a bank application, maintained by many servers. 
Between servers it was used primary-backup replication. 
Above these several bank servers, there are other servers which implement the Paxos Algorithm, to choose one bank server to be the primary in a given time slot.
All the configuration, must be done in the file 'configuration_sample', like the number of servers, and all the time slots. Some servers can be put frozen or suspect by others. The commands from clients, must be written in the client scripts, and the name of the client scripts to be used must be written in the file 'configuration sample' in the line of each client. 
