# Changelog
## 0.2.2
- Fixed event unsubscription not working with event wrapper.
- Add basic unit tests for the event bus.

## 0.2.1
- Fixed highest priority not working properly.

## 0.2.0
- Added priority on event subscription.

## 0.1.0
- Added an event bus (supports interface and thread-safe)
- **IEvent**, **EventBase**, **ScriptableObjectEvent** are what you need for creating an event. 