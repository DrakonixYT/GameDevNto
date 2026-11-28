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
        public static string ToRu(this DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return "Физика";
                case DamageType.Thermal: return "Термо";
                case DamageType.Plasma: return "Плазма";
                case DamageType.Corrosive: return "Кислота";
                default: return type.ToString();
            }
        }
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
