using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public class BlueprintsModEntries
  {

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterStartup)]
    public static void AfterStartup()
    {
      Pipliz.Log.Write("Loaded Blueprints Mod 3.0 by Scarabol");
      // FIXME scan for blueprints
      string[] files = Directory.GetFiles("gamedata/mods/Scarabol/Blueprints/blueprints/", "**.json", SearchOption.AllDirectories);
      foreach (string filename in files)
      {
        JSONNode json;
        if (Pipliz.JSON.JSON.Deserialize(filename, out json, false))
        {
          if (json != null)
          {
            try
            {
              string name = json["name"].GetAs<string>();
              List<BlueprintBlock> blocks = new List<BlueprintBlock>();
              Pipliz.Log.Write(string.Format("Reading blueprint {0} from {1}", name, filename));
              foreach (JSONNode node in json["blocks"].LoopArray())
              {
                int startx = getJSONInt(node, "startx", "x", 0, false);
                int starty = getJSONInt(node, "starty", "y", 0, false);
                int startz = getJSONInt(node, "startz", "z", 0, false);
                string typename;
                try
                {
                  typename = node["typename"].GetAs<string>();
                }
                catch (Exception)
                {
                  try
                  {
                    typename = node["t"].GetAs<string>();
                  }
                  catch (Exception)
                  {
                    throw new Exception(string.Format("typename not defined or not a string"));
                  }
                }
                int width = getJSONInt(node, "width", "w", 1, true);
                int height = getJSONInt(node, "height", "h", 1, true);
                int depth = getJSONInt(node, "depth", "d", 1, true);
                for (int x = startx; x < startx + width; x++)
                {
                  for (int y = starty; y < starty + height; y++)
                  {
                    for (int z = startz; z < startz + depth; z++)
                    {
                      blocks.Add(new BlueprintBlock(x, y, z, typename));
                    }
                  }
                }
              }
              BlueprintsReplaceBlockCode.blueprints.Add("Blueprint "+name, blocks);
            }
            catch (Exception exception)
            {
              Pipliz.Log.Write(string.Format("Exception loading from {0}; {1}", filename, exception.Message));
            }
          }
        }
      }
    }

    private static int getJSONInt(JSONNode node, string name, string alternativeName, int defaultValue, bool optional)
    {
      try
      {
        return node[name].GetAs<int>();
      }
      catch (Exception)
      {
        try
        {
          return node[alternativeName].GetAs<int>();
        }
        catch (Exception)
        {
          if (optional)
          {
            return defaultValue;
          }
          else
          {
            throw new Exception(string.Format("Neither {0} nor {1} defined or not an integer", name, alternativeName));
          }
        }
      }
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterAddingBaseTypes)]
    public static void AfterAddingBaseTypes ()
    {
      foreach (string key in BlueprintsReplaceBlockCode.blueprints.Keys)
      {
        ItemTypes.AddRawType(key,
          new JSONNode(NodeType.Object)
            .SetAs("isSolid", "false")
            .SetAs("isRotatable", "true")
            .SetAs("rotatablex+", key+"x+")
            .SetAs("rotatablex-", key+"x-")
            .SetAs("rotatablez+", key+"z+")
            .SetAs("rotatablez-", key+"z-")
            .SetAs("npcLimit", "0")
        );
        ItemTypes.AddRawType(key+"x+",
          new JSONNode(NodeType.Object)
            .SetAs("parentType", key)
        );
        ItemTypes.AddRawType(key+"x-",
          new JSONNode(NodeType.Object)
            .SetAs("parentType", key)
        );
        ItemTypes.AddRawType(key+"z+",
          new JSONNode(NodeType.Object)
            .SetAs("parentType", key)
        );
        ItemTypes.AddRawType(key+"z-",
          new JSONNode(NodeType.Object)
            .SetAs("parentType", key)
        );
      }
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesServer)]
    public static void AfterItemTypesServer ()
    {
      foreach (string key in BlueprintsReplaceBlockCode.blueprints.Keys)
      {
        ItemTypesServer.RegisterType(key,
          new ItemTypesServer.ItemActionBuilder()
            .SetOnAdd(BlueprintsReplaceBlockCode.OnAdd)
        );
      }
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad)]
    public static void AfterWorldLoad()
    {
      foreach (KeyValuePair<string, List<BlueprintBlock>> entry in BlueprintsReplaceBlockCode.blueprints)
      {
        try
        {
          Dictionary<ushort, int> matsmap = new Dictionary<ushort, int>();
          foreach (BlueprintBlock block in entry.Value) {
            ushort typeIndex = ItemTypes.IndexLookup.GetIndex(block.typename);
            int val;
            if (!matsmap.TryGetValue(typeIndex, out val))
            {
              matsmap.Add(typeIndex, 1);
            }
            else
            {
              matsmap[typeIndex] = val+1;
            }
          }
          List<InventoryItem> mats = new List<InventoryItem>();
          foreach (KeyValuePair<ushort, int> matentry in matsmap) {
            mats.Add(new InventoryItem(matentry.Key, matentry.Value));
          }
          RecipeCraftingStatic.AllRecipes.Add(new RecipeCrafting(true, mats, new List<InventoryItem> { new InventoryItem(ItemTypes.IndexLookup.GetIndex(entry.Key), 1) }));
        }
        catch (Exception exception)
        {
          Pipliz.Log.Write("Exception loading material type;" + exception.Message);
        }
      }
    }
  }

  public class BlueprintBlock
  {
    public int offsetx;
    public int offsety;
    public int offsetz;
    public string typename;

    public BlueprintBlock(int offsetx, int offsety, int offsetz, string typename)
    {
      this.offsetx = offsetx;
      this.offsety = offsety;
      this.offsetz = offsetz;
      this.typename = typename;
    }
  }

  static class BlueprintsReplaceBlockCode
  {

    public static Dictionary<string, List<BlueprintBlock>> blueprints = new Dictionary<string, List<BlueprintBlock>>();

    public static void OnAdd(Vector3Int position, ushort newType, NetworkID causedBy)
    {
      //Chat.Send(causedBy, string.Format("You placed block {0} at {1}", ItemTypes.IndexLookup.GetName(newType), position));
      ThreadManager.InvokeOnMainThread(delegate ()
      {
        ushort actualType;
        if (!World.TryGetTypeAt(position, out actualType) || actualType != newType)
        {
          return;
        }
        ServerManager.TrySetBlock(position, ItemTypes.IndexLookup.GetIndex("air"), causedBy, false);
        string fullname = ItemTypes.IndexLookup.GetName(newType);
        string name = fullname.Substring(0, fullname.Length-2);
        ushort hxm = ItemTypes.IndexLookup.GetIndex(name+"x-");
        ushort hzp = ItemTypes.IndexLookup.GetIndex(name+"z+");
        ushort hzm = ItemTypes.IndexLookup.GetIndex(name+"z-");
        List<BlueprintBlock> blocks;
        blueprints.TryGetValue(name, out blocks);
        foreach (BlueprintBlock blueblock in blocks)
        {
          int realx = blueblock.offsetz;
          int realz = -blueblock.offsetx;
          if (newType == hxm)
          {
            realx = -blueblock.offsetz;
            realz = blueblock.offsetx;
          }
          else if (newType == hzp)
          {
            realx = blueblock.offsetx;
            realz = blueblock.offsetz;
          }
          else if (newType == hzm)
          {
            realx = -blueblock.offsetx;
            realz = -blueblock.offsetz;
          }
          ServerManager.TrySetBlock(position.Add(realx, blueblock.offsety, realz), ItemTypes.IndexLookup.GetIndex(blueblock.typename), causedBy, false);
        }
      }, 3.0);
    }
  }
}
