﻿namespace Owin.Scim.Registry
{
    using System.ComponentModel.Composition;

    using Configuration;

    using DryIoc;

    using NContext.Security.Cryptography;

    using Repository;
    using Repository.InMemory;

    using Security;

    using Services;

    using Validation.Users;

    public class Registry : IConfigureDryIoc
    {
        private readonly IManageCryptography _CryptograhyManager;

        [ImportingConstructor]
        public Registry([Import]IManageCryptography cryptograhyManager)
        {
            _CryptograhyManager = cryptograhyManager;
        }

        public int Priority
        {
            get { return 0; }
        }

        public void ConfigureContainer(IContainer container)
        {
            // TODO: (DG) Create extensibility points for ScimServerConfiguration to register the below impl.
            container.RegisterDelegate<IProvideHashing>(r => _CryptograhyManager.HashProvider);
            container.Register<ISchemaTypeFactory, DefaultSchemaTypeFactory>(Reuse.Singleton);
            container.Register<IUserRepository, InMemoryUserRepository>(Reuse.InWebRequest);
            container.Register<IManagePasswords, DefaultPasswordManager>(Reuse.Singleton);
            container.Register<IVerifyPasswordComplexity, DefaultPasswordComplexityVerifier>(Reuse.Singleton);
            container.Register<IUserService, UserService>(Reuse.Singleton);
            container.Register<UserValidatorFactory>(Reuse.Singleton);
        }
    }
}