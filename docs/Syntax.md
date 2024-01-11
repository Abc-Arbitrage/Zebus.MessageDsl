
# Message DSL Syntax

## Basic syntax

To define a message class, write its constructor in C# syntax:

```C#
CreateStuffCommand(string name);
StuffCreated(string name);
```

Whitespace characters are mostly not significant, so you can write a single message definition on several lines:

```C#
Foo(
    int a,
    int b,
    int c
);
```

Semicolons are optional at the end of a line.

The ProtoBuf members and their tags will be deduced from the constructor (see below for details). Member names will be PascalCased.

As a rule of thumb, when in doubt about how to write something, try the C# syntax.

## Comments

C# syntax is used for comments:

```C#
// This is a line comment
/* This is an inline comment */
```

## Namespace

You can override the default namespace with a `namespace` clause:

```C#
namespace Foo.Bar;

SomeMessage(int a);
```

Note that this clause is *not* followed by braces, and takes effect for the whole file - you can't define multiple namespaces in the same file.

## Namespace imports

Write a `using` directive to import a namespace:

```C#
using SomeOtherLibrary;
```

The following namespaces are imported by default:

 - `System`
 - `ProtoBuf`
 - `Abc.Zebus`
 - `Abc.Zebus.Routing` if a routable message is defined
 - `System.Collections.Generic` if `List<T>` is used
 - `System.ComponentModel` if the `[Description]` attribute is used

## Message types

If the message name ends with `Command`, it will implement `ICommand`, otherwise it will implement `IEvent`. This can be overridden with the following syntax:

```C#
DoStuff(int id) : ICommand;
```

Additional base types can be specified using the same syntax.

Add a `!` suffix to a message type in order to implement `IMessage` instead of either `ICommand` or `IEvent`. These types can be used as inner message types, or as reply messages.

```C#
Error!(int errorCode, string message);
ErrorsDetected(int entityId, Error[] errors);
```

## ProtoBuf tags

ProtoBuf tags are assigned implicitly by default, in increasing order.

> [!WARNING]
> It is dangerous to add/remove/move message members without taking their tags into consideration. Tags define the wire format of the message.
> 
> When removing a member, replace it with a discard (`_`) to preserve the tags of the following ones and to document the fact that a member has been removed.
> A `[ProtoReserved]` attribute will also be added for each range of discarded parameters.

Tags can be redefined using the `[N]` syntax, where `N` is the desired tag number:

```C#
Foo(int a, [4] int b, int c);
```

Which is equivalent to:

```C#
Foo(int a, _, _, int b, int c);
```

Implicit tag numbering resumes after an explicitly defined tag. The tags for the previous examples will be: 

 - `a`: 1
 - `b`: 4
 - `c`: 5

Alternatively, you can also use the full `[ProtoMember(N)]` syntax:

```C#
Foo(int a, [ProtoMember(4)] int b, int c);
```

This will yield the same result as above.

## IsRequired

You can set the `IsRequired` parameter in the `ProtoMember` attribute to `false` by appending a `?` character to the member *name*:

```C#
Foo(int a?);
```

Note that this is different from:

```C#
Foo(int? a);
```

Which uses a nullable `int`, although both examples will have `IsRequired = false`, since the default value depends on the member type (repeated and nullable members will not be required).

## Default values

Default parameter values can be specified:

```C#
Foo(int a = 42);
```

Default values are only used in the constructor. They have no influence over what is transmitted on the wire, nor on the deserialized values in case a member is missing.

## Attributes

Attributes can be specified for messages, message members and for enums:

```C#
[Transient]
Foo(int a, [Obsolete] int b)
```

Note that the `[Obsolete]` attribute will go on the member property by default, *not* on the constructor parameter.

You can customize the targets of attributes using the same syntax as in C#:

```C#
Foo(int a, [param: Obsolete] int b)
```

The `[Obsolete]` attribute will now be applied to the constructor parameter only, not to the associated member property.

The supported attribute targets are:

| Target     | Usable on          | Default | Will apply on         |
|------------|--------------------|---------|-----------------------|
| `type`     | Messages and enums | Yes     | Message class or enum |
| `param`    | Message members    | No      | Constructor parameter |
| `property` | Message members    | Yes     | Member property       |
| `field`    | Enum fields        | Yes     | Enum field            |

Only the `param` target will change the default behavior. The other targets are available for consistency.

## Routable messages

Use attributes to define routable messages:

```C#
[Routable]
Foo([RoutingPosition(1)] int id);
```

The `Abc.Zebus.Routing` namespace will be imported automatically.

## Generic messages

Messages can be generic and have constraints:

```C#
EntityUpdated<TEntity>(int entityId) where TEntity : IEntity;
```

## Access modifiers

Messages and enums are `public` by default, except in a `#pragma internal` scope. Accessibility can be set explicitly using the `public` or `internal` keywords:

```C#
internal Foo(int a);
public Bar(int b);

internal enum Color {
    Red,
    Green,
    Blue
}
```

## Message options

Messages can be prefixed with the following keywords: `public`, `internal`, `sealed`, `abstract`.

Use the `#pragma` directive to enable or disable flags on a specific scope. The scope starts at the directive.

```C#
Foo(int id);

#pragma mutable
Bar(int id);
#pragma !mutable

Baz(int id);
```

The `Bar` message is marked as mutable.

The available options are:

 - `#pragma mutable` - specifies if messages are to be generated as mutable (public setters on properties) instead of read-only (private setters).
 - `#pragma proto` - declares messages for export to a `.proto` file.
 - `#pragma internal` - sets the default accessibility to `internal`.
 - `#pragma public` - sets the default accessibility to `public` (default, same effect as `#pragma !internal`).
 - `#pragma nullable` - enables support for nullable reference types.

## Inheritance

A message type can inherit from another message type. The base type parameters will then be included in the constructor, unless the base type is marked as mutable.

```C#
Foo(int fooId) : Bar;
Bar(int barId) : Baz;
Baz(int bazId);
```

The constructor of `Foo` will be `Foo(int bazId, int barId, int fooId)`, and the constructor of `Bar` will be `Bar(int bazId, int barId)`.

## Nested classes

A message can be emitted as a nested class using the following syntax:

```C#
Foo.Bar.Baz(int id);
```

The `Baz` message class will be nested in `Bar`, which will itself be nested in `Foo`. All containing types need to be classes.

## Enums

Enums can be declared just as in C#:

```C#
enum Color {
    Red,
    Green,
    Blue = 42
};

ChangeColorCommand(int id, Color color);
```
