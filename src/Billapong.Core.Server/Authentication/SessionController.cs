﻿namespace Billapong.Core.Server.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.ServiceModel;
    using System.Text;
    using Billapong.Contract.Data.Authentication;
    using Billapong.Contract.Exceptions;
    using Billapong.DataAccess.Model.Session;
    using Billapong.DataAccess.Repository;

    public class SessionController
    {
        private const string Salt = "B1ll4p0ng";

        private static readonly object LockObject = new object();
        
        private readonly IRepository<User> userRepository;

        private readonly IDictionary<Role, IList<Guid>> sessionStore; 

        #region Singleton Implementation

        /// <summary>
        /// Initializes static members of the <see cref="SessionController"/> class.
        /// </summary>
        static SessionController()
        {
            Current = new SessionController();
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="SessionController"/> class from being created.
        /// </summary>
        private SessionController()
        {
            this.userRepository = new Repository<User>();
            this.sessionStore = new Dictionary<Role, IList<Guid>>
                                    {
                                        { Role.Administrator, new List<Guid>() },
                                        { Role.Editor, new List<Guid>() }
                                    };
        }

        /// <summary>
        /// Gets the current instance.
        /// </summary>
        /// <value>
        /// The current instance.
        /// </value>
        public static SessionController Current { get; private set; }

        #endregion

        public Guid Login(string username, string password, Role role)
        {
            var hash = GetPasswordHash(password);
            var user = this.userRepository.Get(dbUser => dbUser.Username == username
                && dbUser.Password == hash
                && dbUser.Role == (int)role);

            if (!user.Any())
            {
                throw new FaultException<LoginFailedException>(new LoginFailedException(username), "Login failed");
            }

            var sessionId = Guid.NewGuid();
            lock (LockObject)
            {
                this.sessionStore[role].Add(sessionId);    
            }
            
            return sessionId;
        }

        public void Logout(Guid sessionId)
        {
            lock (LockObject)
            {
                foreach (var store in this.sessionStore.Values)
                {
                    store.Remove(sessionId);
                }
            }
        }

        public bool IsValidSession(Guid sessionId, Role role)
        {
            lock (LockObject)
            {
                return this.sessionStore[role].Contains(sessionId);
            }
        }

        public static string GetPasswordHash(string password)
        {
            var salted = string.Join("_", password, Salt);
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(salted));
                var stringBuilder = new StringBuilder();
                foreach (var element in hash)
                {
                    stringBuilder.Append(element.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }
    }
}