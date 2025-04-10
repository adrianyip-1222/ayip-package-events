# AYip Events

## 1. Event Bus Setup
You can choose one of these three ways (but not limited to...) to instantiate an event bus.
### Event Bus (simplest way)
```C#
// Pass around the eventbus reference by yourself.
var eventBus = new EventBus();
```

### Event Bus (with dependency injection, Zenject / Extenject)
```C#
public class EventBusInstaller : Installer<EventBusInstaller>
{
    public override void InstallBindings()
    {
        // You can have multiple event buses
        // So it can be not in singleton.
        Container.Bind<EventBus>().AsSingle();
    }
}
```

### Event Bus (with dependency injection, VContainer)
```C#
public class FooLifetimeScope : VContainer.Unity.LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // You can have multiple event buses
        // So it can be not in singleton.
        builder.Register<EventBus>(Lifetime.Singleton);
    }
}
```

---
## 2. Events Setup
Your custom events must inherit IEvent to work. It basically stores the event ID and timestamp for general purpose.

**EventBase** is a disposable base class that does the ID and timestamp assignment for you.
### Events (disposable object, simplest)
```C#
public class FooEvent : EventBase
{
    public FooEvent(Foo foo)
    {
        Foo = foo;
    }

    public Foo Foo { get; }
}

```
### Events (readonly struct, parameterless)
When it comes with struct, you need to handle the ID and timestamp on your own.
```C#
public readonly struct FooEvent : IEvent
{
    // Use factory pattern for creating a parameterless constructor
    public static FooEvent Create()
    {
        return new FooEvent(Guid.NewGuid(), DateTime.Now);
    }
    
    private FooEvent(Guid id, DateTime timestamp)
    {
        Id = id;
        Timestamp = timestamp;
    }
    
    public Guid Id { get; }
    public DateTime Timestamp { get; }
}
```
### Events (readonly struct, with parameters)
```C#
public readonly struct FooEvent : IEvent
{   
    public FooEvent(string name)
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.Now;
        Name = name;
    }
    
    public Guid Id { get; }
    public DateTime Timestamp { get; }
    public string Name { get; }
}
```
### ScriptableObject Events (Unity)
Inherit the **ScriptableObjectEvent** for your events.
```C#
[CreateAssetMenu (menuName="path/FooSoEvent")]
public class FooSoEvent : ScriptablObjectEvent
{
    // Only for demonstration.
    public string SomeDataToSetOnInspector;
    
    protected override Awake ()
    {
        // Do your own handling when this SO is instantiated.
        // Debug.Log (SomeDataToSetOnInspector);
    }
}
```

---
## 3. Event Subscription

### Unity Example
```C#
// Get your event bus from somewhere.
// Or use [Inject] to resolve it if you use DI (dependency injection)
private EventBus _eventBus;

private void OnEnable ()
{
    _eventBus.Subscribe<FooEvent>(OnFooPublished);
    
    // Subscribe the event with priority.
    // Higher priority of events will get executed first.
    _eventBus.Subscribe<FooEvent>(OnFooPublished, Priority.Highest);
    _eventBus.Subscribe<FooEvent>(OnFooPublished, priority: 999);
}

private void OnEnable ()
{
    _eventBus.Unsubscribe<FooEvent>(OnFooPublished);
}

private void OnFooPublished (FooEvent eventData)
{
    // Do something with the eventData.
    // var data = eventData.someData;
}
```

### Subscribe to an interface
Every published event with this interface will call the handler.
```C#
// Basically change the type to the interace
_eventBus.Subscribe<IFooEvent>(OnIFooPublished);
_eventBus.Unsubscribe<IFooEvent>(OnIFooPublished);
private void OnIFooPublished (IFooEvent eventData) { }
```

---
## Event Publish

```C#
// Get your event bus from somewhere.
// Or use [Inject] to resolve it if you use DI (dependency injection)
private EventBus _eventBus;

// For demostration below.
public FooSoEvent soEvent;

private void SomeFunction ()
{
    ///////////////////////////////////// Usage
    // Simplest object, auto-dispose
    _eventBus.Publish (new FooEvent ());
    _eventBus.Publish (new FooEvent (), autoDispose: true);
    
    // Simplest object w/o auto-dispose
    _eventBus.Publish (new FooEvent (), false);
    _eventBus.Publish (new FooEvent (), autoDispose: false);
    
    // Publish asynchronously 
    // (experimental feature, not yet supported cancellation)
    _eventBus.PublishAsync (new FooEvent ());
    _eventBus.PublishAsync (new FooEvent (), autoDispose: true);
    
    // Readonly struct w/ parameterless
    _eventBus.Publish (FooEvent.Create());
    
    // Readonly struct w/ parameters
    _eventBus.Publish (new FooEvent("Some data"));
    
    // Unity Specific
    // ScriptableObject w/o a reference.
    var so1 = ScriptableObject.CreateInstance<ScriptableObjectEvent>();
    _eventBus.Publish (so1);
    
    // ScriptableObject w/ a reference.
    var so2 = Instantiate (soEvent);
    _eventBus.Publish (so2);
    
    ////////////////////////////////////////////////////// Return value
    // Publish function returns a boolean to indicate
    // whether if the event has any subscribers.
    var hasSubscribers = _eventBus.Publish (new FooEvent());
    if (!hasSubscribers)
    {
        // Do your stuff...
        // Release some resources for example.
    }
    
}
```