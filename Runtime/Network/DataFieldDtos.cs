﻿using System;

namespace mazing.common.Runtime.Network
{
    public class GameFieldDtoLite
    {
        public int AccountId { get; set; }
        public ushort FieldId { get; set; }
        public int GameId { get; set; }
    }
    
    public class GameFieldDto : GameFieldDtoLite
    {
        public object Value { get; set; }
        public DateTime LastUpdate { get; set; }

        public GameFieldDto()
        {
            LastUpdate = DateTime.Now;
        }
    }
}