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
