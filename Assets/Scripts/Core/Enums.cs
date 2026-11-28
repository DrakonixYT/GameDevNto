namespace HangarGame.Core
{
    public enum DamageType
    {
        Physical,
        Thermal,
        Plasma,
        Corrosive
    }

    public static class DamageTypeExtensions
    {
        public static string ToRu(this DamageType type) => type switch
        {
            DamageType.Physical => "Физика",
            DamageType.Thermal => "Термо",
            DamageType.Plasma => "Плазма",
            DamageType.Corrosive => "Кислота",
            _ => type.ToString()
        };
    }

    public enum PartSlot
    {
        Torso,
        Head,
        LeftArm,
        RightArm,
        Legs
    }

    public enum RoomView
    {
        Hangar,
        DispatchConsole,
        WorkbenchFocus
    }

    public enum ArmorySlot
    {
        LeftArm,
        RightArm
    }
}
