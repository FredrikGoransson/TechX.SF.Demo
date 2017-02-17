# TechX.SF.Demo
Service Fabric demo showing a basic application using Reliable Services and Actors

Guide for setting up a simple Service Fabric application that uses Stateless Services, Actors/ActorServices and fabric communication.

## Create a WebApi project

Start by creating a new Visual Studio project based on the template ``Cloud/Service Fabric Application``.

Update the ``Controllers/ValueController`` by renaing it to ``HomeController`` and delete all ControllerActions except ``Get()``.

## Add a new Stateless Service project

### Add web scraper class

Right-click the Service Fabric Application project and select ``Add->New Service Fabric Service``. Select ``Stateless Sercice`` as service type to create.

Add the ``Scraper.cs`` class to the project and add the code from [WebScraperService/Scraper.cs](/02-create-stateless-service/src/TechX.SF.Demo/WebScraperService/Scraper.cs)

### Call scraper in RunAsync

RunAsync in a Reliable Service will start running as soon as the service has been deployed and started. This is a great place to run our web scraper from in a periodic loop.

Add the following code to the block within the infinite ``while(true)`` loop:

```csharp
var speakers = await scraper.ScanSpeakers();

foreach (var speaker in speakers)
{
  if (!readSpeakers.Contains(speaker.GetShortHash()))
  {
    ServiceEventSource.Current.ServiceMessage(this.Context, $"Found new/updated speaker {speaker.Name}");
  }
}

var sessions = await scraper.ScanSessions();
scraper.AddSessionsToSpeakers(speakers, sessions);

foreach (var speaker in speakers)
{
  ServiceEventSource.Current.ServiceMessage(this.Context, $"Updating {speaker.Sessions.Count} sessions for speaker {speaker.Name}");
}
```


This call the scraper for each iteration and for each session and writes an ETW event for each found speaker and the sessions for each speaker found.


