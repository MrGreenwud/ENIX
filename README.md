# ENIX
ENIX is a independent object serializer designed to simplify data serialization with key features like object type retention and reference restoration. Originally developed as part of the Enigmatic for Unity, ENIX has evolved into a standalone tool.

### Features
- Independent of Unity: ENIX is a general-purpose serializer and does not rely on Unity-specific types or libraries.
- Object Type Preservation:
- Retains object type information during serialization.
- Reference Restoration: Rebuilds object references during deserialization.
- Simple Integration: Easily integrates into any C# project.

### Installation
1. Download the .dll library from the release.
2. Add the ENIX.dll and include in your project.
3. Reference the ENIX namespace in your code.

# How to use?
## Serialization
1. Create your class and mark the fields and/or properties with the SerializebleProperty attribute that you want to serialize
##### Exemple:
```csharp
public class Character
{
    [SerializebleProperty] private string _name;
    [SerializebleProperty] private int _hp;
    [SerializebleProperty] private Weapon _weapon;

    public Character(string name, int hp, Weapon weapon)
    {
        _name = name;
        _hp = hp;
        _weapon = weapon;
    }
}

public class Weapon
{
    [SerializebleProperty] private int _damage;

    public Weapon(int damage)
    {
        _damage = damage;
    }
}
```
2. Create instances of your classes and add them to an array of objects.
##### Exemple:
```csharp
Weapon weapon = new Weapon(20);
Character character = new Character("Bob", 100, weapon);

object[] objects = { character };
```
3. Serialize your objects using the ENIXSerializer class and the Serialize method. 
###### This can be done in several ways
1. Serialization of classes with obtaining the result as a list of string: You can serialize your classes and get the result as a list of strings.
##### Exemple:
```csharp
List<string> serializedObjects = ENIXSerializer.Serialize(objects);
```
2. Serialization to file: You can specify file names, an array of objects, and a path to save the serialized data to a file with a .enix extension.
##### Exemple:
```csharp
ENIXSerializer.Serialize("FileName", objects, "path");
```
## Deserialization
1. For deserialization, use the ENIXDeserializer class and the Deserialize method. It will return an array of objects.
###### This can be done in several ways
1. Deserialization of the list of serialized objects: You can pass a list of strings that are serialized objects.
##### Exemple:
```csharp
object[] objects = ENIXDeserializer.Deserialize(serializedObjects);
```
2. You can read data from a .enix file and deserialize the objects.
##### Exemple:
```csharp
string file = File.ReadAllText(path);
object[] objects = ENIXDeserializer.Deserialize(file);
```
## Customize
You can add your own unique way of serializing and deserializing a specific class/structure type and extend the capabilities of the serializer like [UnIX](https://github.com/MrGreenwud/UnIX) does (ENIX extensions for Unity). 
To work during program initialization, run registering custom serializers/deserializers.
```csharp
RegisterCustomSerializer.Register();
```
To create your own serializer or deserializer, create a static class and mark it with the CustomSerializer attribute and create a static method. 

If you want to make a serializing method, then this method must return a string (serialized data), accept arguments: object? property, string name, Type type, be marked with the CustomPropertySerializerMethod attribute with the corresponding type
##### Exemple:
```csharp
[CustomSerializer]
internal static class CustomSerializerExample
{
    [CustomPropertySerializerMethod(typeof(Player))]
    public static string SerializeExample(object? property, string name, Type type) 
    {
        //Serialize
        return serializedObject;
    }
}
```
For serialization you can use templates from the ENIXSerializer class such as:
- SerializeArray(object? property, string name)
- SerializeDictionary(object? property, string name, Type type)
- SerializeList(object? property, string name, Type type)
- SerializeEnum(object? property, string name)
- SerializeStruct(object property, string name, FieldInfo[] fields, bool isSerializedProperty = true)
###### isSerializedProperty is a flag that indicates whether the serializer will take into account whether fields are marked with the SerializebleProperty attribute

If you want to make a deserializing method, then this method must return an object (deserialized), take an argument: string serializedObject, be marked with a CustomPropertyDeserializerMethod attribute with the appropriate type
##### Exemple:
```csharp
[CustomSerializer]
internal static class CustomDeserializerExample
{
    [CustomPropertyDeserializerMethod(typeof(Player))]
    public static object DeserializExample(string serializedObject)
    {
        //Deserializ
        return obj;
    }
}
```

For deserialization, you can use templates from the ENIXDeserializer class, such as:
- DeserializeArray(string serializedProperty, Type propertyType)
- DeserializeList(string serializedProperty, Type propertyType)
- DeserializeDictionary(string serializedProperty, Type propertyType)
- DeserializeStruct(string serializedProperty, Type propertyType)
- DeserializeEnum(string serializedProperty, Type propertyType)

### Important! 
If you serialize an object type in a unique way, you need to create your own unique deserialization method for that type.
