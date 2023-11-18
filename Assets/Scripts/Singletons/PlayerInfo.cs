using System;
using Unity.Netcode;

public struct PlayerInfo : INetworkSerializable, IEquatable<PlayerInfo> {
    public ulong id;
    public TeamColor teamColor;
    public Character character;

    public PlayerInfo(ulong _id, TeamColor _teamColor, Character _character) {
        id = _id;
        teamColor = _teamColor;
        character = _character;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref teamColor);
        serializer.SerializeValue(ref character);
    }

    public bool Equals(PlayerInfo other) {
        return id == other.id &&
               teamColor == other.teamColor &&
               character == other.character;
    }
}