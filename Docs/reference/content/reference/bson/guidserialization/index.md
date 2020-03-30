+++
date = "2020-03-30T14:34:00Z"
draft = false
title = "Guid Serialization"
[menu.main]
  parent = "BSON"
  identifier = "Guid Serialization"
  weight = 50
  pre = "<i class='fa'></i>"
+++

## Background information

Guids were originally represented in BSON as BsonBinaryData values of subtype 3. Unfortunately, different drivers
inadvertently used different byte orders when converting the Guid to a 16 byte binary value. To standardize on a
single canonical order BsonBinaryData subtype 4 was created with a well defined byte order.

The C# driver's support for Guids was originally based on the premise that all Guids in a single collection must
be represented the same way (i.e. using the same BsonBinaryData sub type and byte order). In order to accomplish this
the representation of Guids is enforced at the BSON reader and writer levels (because a single reader or writer is
used to read or write an entire document from or to the collection).

However, this original premise has not stood the test of time.

The first issue we ran into was when the server
started returning UUIDs (i.e. Guids) in metadata using standard subtype 4. If a collection was configured to use
subtype 3 (which it usually is since that is the default) the driver could not deserialize the Guids in the metadata
without throwing an exception. We worked around this by temporarily reconfiguring the BSON reader to not expect that
Guids would be in subtype 3 while reading the metadata.

The second issue is that the original premise is too strict. There are valid reasons why a single collection might
have a mix of Guid representations, and we need to allow that.

However, if we just change the driver's behavior abruptly that would be a breaking change. In order to help applications
migrate in an orderly fashion to the new way of handling Guids we have introduced a configurable `GuidRepresentationMode`.
In V2 mode the driver will handle Guids the same way that the v2.x versions have historically. In V3 mode the driver
will handle Guids in the new way. An application can opt in to V3 mode to transition to the new way Guids are handled.
In the v2.x versions of the driver V2 is the default mode but V3 mode is supported. In future v3.x versions of the driver
V3 will be the default mode (and support for V2 mode will be removed).

## V2 Guid Representation Mode (Deprecated)

## V3 Guid Representation Mode

## Summary of changes from V2 to V3 Guid Representation Mode


