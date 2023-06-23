﻿using System;

namespace ElementEngine.ECS
{
    public class Group
    {
        public Registry Registry;
        public Type[] Types;
        public Type[] ExcludeTypes;
        public int EntityCount = 0;
        public Entity[] EntityBuffer;
        public SparseSet EntityLookup;

        public Group(Registry registry, ReadOnlySpan<Type> types)
        {
            Registry = registry;
            EntityLookup = new SparseSet(1000);
            Types = types.ToArray();
            EntityBuffer = new Entity[100];
        }

        public ReadOnlySpan<Entity> Entities => new ReadOnlySpan<Entity>(EntityBuffer, 0, EntityCount);

        public void AddEntity(Entity entity)
        {
            if (EntityLookup.Contains(entity.ID))
                return;

            EntityLookup.TryAdd(entity.ID, out var _);

            if (EntityCount >= EntityBuffer.Length)
                Array.Resize(ref EntityBuffer, EntityBuffer.Length * 2);

            EntityBuffer[EntityCount++] = entity;
        }

        public void RemoveEntity(Entity entity)
        {
            if (!EntityLookup.Contains(entity.ID))
                return;

            EntityLookup.Remove(entity.ID);

            var entityIndex = 0;

            for (var i = 0; i < EntityCount; i++)
            {
                if (EntityBuffer[i] == entity)
                    entityIndex = i;
            }

            EntityBuffer[entityIndex] = EntityBuffer[EntityCount - 1];
            EntityBuffer[EntityCount - 1] = default;
            EntityCount -= 1;
        }
    }
}
