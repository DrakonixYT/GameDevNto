namespace HangarGame.Core
{
    public enum DamageType
    {
        Physical,   // Кинетический / пули / снаряды
        Thermal,    // Огонь / огнемет / термо-оружие
        Plasma,     // Энергия / щитобой
        Corrosive   // Кислота / био-урон монстров
    }

    public enum PartSlot
    {
        Head,
        Torso,
        LeftArm,
        RightArm,
        Legs
    }

    public enum RoomView
    {
        Hangar,          // 0 градусов — смотрим на мехов и верстак
        DispatchConsole, // 180 градусов — смотрим на терминал управления и карту города
        WorkbenchFocus   // Приближение к верстаку для ремонта детали
    }

    public enum GameState
    {
        Peaceful,
        ThreatIncoming,
        MissionActive,
        Repairing,
        Victory,
        GameOver
    }

    public enum RepairToolType
    {
        None,
        Wrench,    // Гаечный ключ / шуруповерт (болты)
        Welder     // Сварочный аппарат (трещины)
    }
}
