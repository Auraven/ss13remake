﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using GorgonLibrary;

namespace CGO
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager
    {
        private EntityFactory m_entityFactory;
        private EntityTemplateDatabase m_entityTemplateDatabase;
        private EntityNetworkManager m_entityNetworkManager;
        private NetClient m_netClient;

        public EntityTemplateDatabase TemplateDB { get { return m_entityTemplateDatabase; } }

        private Dictionary<int, Entity> m_entities;
        private int lastId = 0;

        public EntityManager(NetClient netClient)
        {
            m_entityNetworkManager = new EntityNetworkManager(netClient);
            m_entityTemplateDatabase = new EntityTemplateDatabase();
            m_entityFactory = new EntityFactory(m_entityTemplateDatabase);
            m_entities = new Dictionary<int, Entity>();
            m_netClient = netClient;
            Singleton = this;
        }

        private static EntityManager singleton;
        public static EntityManager Singleton
        {
            get
            {
                if (singleton == null)
                    throw new Exception("Singleton not initialized");
                else return singleton;
            }
            set
            { singleton = value; }
        }

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public Entity GetEntity(int eid)
        {
            if (m_entities.Keys.Contains(eid))
                return m_entities[eid];
            return null;
        }

        /// <summary>
        /// Creates an entity and adds it to the entity dictionary
        /// </summary>
        /// <param name="templateName">name of entity template to execute</param>
        /// <returns>integer id of added entity</returns>
        public int CreateEntity(string templateName)
        {
            //Get the entity from the factory
            Entity e = m_entityFactory.CreateEntity(templateName);
            if (e != null)
            {
                //It worked, add it.
                e.SetNetworkManager(m_entityNetworkManager);
                m_entities.Add(++lastId, e);
                lastId++;
                return lastId;
            }
            //TODO: throw exception here -- something went wrong.
            return -1;
        }

        private Entity SpawnEntity(string EntityType, int Uid)
        {

            Entity e = m_entityFactory.CreateEntity(EntityType);
            if (e != null)
            {
                e.SetNetworkManager(m_entityNetworkManager);
                e.Uid = Uid;
                m_entities.Add(Uid, e);
                lastId = Uid;
                e.Initialize();
                return e;
            }
            return null;
        }

        public Entity[] GetEntitiesInRange(Vector2D position, float Range)
        {
            var entities = from e in m_entities.Values
                           where (position - e.Position).Length < Range
                           select e;

            return entities.ToArray();
        }

        /// <summary>
        /// Adds an atom to the entity pool. Compatibility method.
        /// </summary>
        /// <param name="e">Entity to add</param>
        public void AddAtomEntity(Entity e)
        {
            ///The UID has already been set by the server..
            m_entities.Add(e.Uid, e);
            e.SetNetworkManager(m_entityNetworkManager);
        }

        public void Shutdown()
        {

        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager 
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg"></param>
        public void HandleEntityNetworkMessage(NetIncomingMessage msg)
        {
            IncomingEntityMessage message = m_entityNetworkManager.HandleEntityNetworkMessage(msg);
            m_entities[message.uid].HandleNetworkMessage(message);
        }

        #region Entity Manager Networking
        public void HandleNetworkMessage(NetIncomingMessage msg)
        {
            EntityManagerMessage type = (EntityManagerMessage)msg.ReadInt32();
            switch(type)
            {
                case EntityManagerMessage.SpawnEntity:
                    string EntityType = msg.ReadString();
                    string EntityName = msg.ReadString();
                    int Uid = msg.ReadInt32();
                    Entity e = SpawnEntity(EntityType, Uid);
                    e.name = EntityName;
                    break;
                case EntityManagerMessage.DeleteEntity:
                    break;
            }
        }

        #endregion
    }
}