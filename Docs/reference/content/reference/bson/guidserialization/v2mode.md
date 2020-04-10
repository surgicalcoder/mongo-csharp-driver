+++
date = "2020-04-09T15:36:56Z"
draft = false
title = "V2 GuidRepresentationMode"
[menu.main]
  parent = "Guid Serialization"
  identifier = "v2mode"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## V2 GuidRepresentationMode (Deprecated)

In V2 mode the central principle is that all Guids in a collection must be represented the same way. In order to enforce
this the representation of Guids is not controlled at the individual serializer level, but rather at the reader/writer
level since the same reader/writer is used to read/write an entire document.

All of the following properties in methods are only relevant to V2 mode and are now deprecated:

* BsonDefaults.GuidRepresentation property
* BsonBinaryData implicit conversion to or from Guid
* BsonBinaryData constructor taking a Guid
* BsonBinaryData constructor taking (byte[], BsonBinarySubType, GuidRepresentation)
* BsonBinaryData.GuidRepresentation property
* BsonBinaryData.ToGuid() method with no arguments is only valid when subtype is 4
* BsonValue implicit conversion from Guid or Guid? (Nullable<Guid>)
