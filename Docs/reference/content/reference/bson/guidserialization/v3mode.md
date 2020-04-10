+++
date = "2020-04-09T15:36:56Z"
draft = false
title = "V3 GuidRepresentationMode"
[menu.main]
  parent = "Guid Serialization"
  identifier = "v3mode"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## V3 GuidRepresentationMode

In V3 mode the central principle is that the representation of Guids is controlled at the level of each individual
property of a document by configuring the serializer for that property. The recommendation is that all Guids in a
collection be represented uniformly using the now standard BsonBinaryData subtype 4, but when working with historical
data it is now acceptable for different Guid fields in the same document to be represented differently.

The following existing methods behave differently in V3 mode:

* BsonBinaryReader.ReadBinaryData method does not tag BsonBinaryData with a GuidRepresentation
* BsonBinaryWriter.WriteBinaryData method does not check that BsonBinaryData subtype matches settings.GuidRepresentation


