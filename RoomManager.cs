﻿using DELTARUNITYStandalone.SerializedFiles;
using DELTARUNITYStandalone.VirtualMachine;
using UndertaleModLib.Models;
using EventType = DELTARUNITYStandalone.VirtualMachine.EventType;

namespace DELTARUNITYStandalone;

public static class RoomManager
{
	public static bool ChangeRoomAfterEventExecution = false;

	/// <summary>
	/// The room to change to.
	/// </summary>
	public static Room RoomToChangeTo = null;

	public static Room CurrentRoom;

	public static Dictionary<int, Room> RoomList = new();

	public static bool RoomLoaded = false;

	public static void ChangeToWaitingRoom()
	{
		ChangeRoomAfterEventExecution = false;

		// events could destroy other objects, cant modify during iteration
		var instanceList = new List<GamemakerObject>(InstanceManager.instances);

		foreach (var instance in instanceList)
		{
			if (instance == null)
			{
				continue;
			}

			if (instance.persistent)
			{
				continue;
			}

			GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.Destroy);
			GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.CleanUp);

			DrawManager.Unregister(instance);
			//Destroy(instance.gameObject);
		}

		InstanceManager.instances = InstanceManager.instances.Where(x => x != null && x.persistent).ToList();

		RoomLoaded = false;
		CurrentRoom = RoomToChangeTo;
		RoomToChangeTo = null;

		OnRoomChanged();
	}

	private static void OnRoomChanged()
	{
		CustomWindow.Instance.SetResolution(CurrentRoom.CameraWidth, CurrentRoom.CameraHeight);

		foreach (var layer in CurrentRoom.Layers)
		{
			if (layer.Instances_Objects != null)
			{
				foreach (var item in layer.Instances_Objects)
				{
					InstanceManager.instance_create_depth(item.X, item.Y, layer.LayerDepth, item.DefinitionID);
				}
			}

			if (layer.Assets_LegacyTiles != null && layer.Assets_LegacyTiles.Count != 0)
			{
				foreach (var tile in layer.Assets_LegacyTiles)
				{
					var newTile = new GMTile
					{
						X = tile.X,
						Y = tile.Y,
						Definition = tile.Definition,
						left = tile.SourceLeft,
						top = tile.SourceTop,
						width = tile.SourceWidth,
						height = tile.SourceHeight,
						depth = tile.Depth,
						instanceId = tile.InstanceID,
						XScale = tile.ScaleX,
						YScale = tile.ScaleY,
						Color = tile.Color
					};

					TileManager.Tiles.Add(newTile);
				}
			}
		}

		RoomLoaded = true;
	}

	public static void ChangeRoomAfterEvent(int index)
	{
		ChangeRoomAfterEvent(RoomList[index]);
	}

	public static void ChangeRoomAfterEvent(Room roomName)
	{
		ChangeRoomAfterEventExecution = true;
		RoomToChangeTo = roomName;
		
		if (CurrentRoom != null && CurrentRoom.Persistent)
		{
			// oh god we gotta save the current scene aaaaaaaa
			throw new NotImplementedException();
		}

		// events could destroy other objects, cant modify during iteration
		var instanceList = new List<GamemakerObject>(InstanceManager.instances);

		foreach (var instance in instanceList)
		{
			if (instance == null)
			{
				continue;
			}

			GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.Other, (int)EventSubtypeOther.RoomEnd);
		}
	}

	public static void room_goto_next()
	{
		ChangeRoomAfterEvent(RoomList[RoomList.Values.ToList().IndexOf(CurrentRoom) + 1]);
	}

	public static void room_goto_previous()
	{
		ChangeRoomAfterEvent(RoomList[RoomList.Values.ToList().IndexOf(CurrentRoom) - 1]);
	}

	public static int room_next(int numb)
	{
		if (RoomList.Count > numb + 1)
		{
			return numb + 1;
		}

		return -1;
	}

	public static int room_previous(int numb)
	{
		if (numb == 0)
		{
			return -1;
		}

		return numb - 1;
	}
}
