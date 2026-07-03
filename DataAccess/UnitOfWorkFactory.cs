using DataAccess.Providers;
using DataAccess.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class UnitOfWorkFactory : IUnitOfWork, IDisposable
    {
        #region Fields
        private IDataContext _context;
        private static Utilities.ServerTypes _serverType;
        private Utilities.RepoTypes _repositoryType;
        #endregion Fields

        #region Properties
        public Utilities.RepoTypes RepositoryType
        {
            get
            {
                return _repositoryType;
            }

            private set
            {
                _repositoryType = value;
            }
        }

        public static Utilities.ServerTypes ServerType
        {
            get
            {
                return _serverType;
            }

            private set
            {
                _serverType = value;
            }
        }

        #endregion Properties

        private UnitOfWorkFactory(IDataContext context, Utilities.RepoTypes repositoryType, Utilities.ServerTypes server = Utilities.ServerTypes.MsSqlServer)
        {
            ServerType = server;
            RepositoryType = repositoryType;
            _context = context;
        }

        public static IUnitOfWork CreateContext()
        {
            string connectionString;
            UnitOfWorkFactory unitOfWork = null;

            switch (ServerType)
            {
                case Utilities.ServerTypes.MsSqlServer:
                    connectionString = Environment.GetEnvironmentVariable("ADLE_MSSQL_CONNECTION")
                        ?? @"Server=localhost;Database=ADLE_Sim_2;Trusted_Connection=True";
                    unitOfWork = new UnitOfWorkFactory(new EntityFrameworkProvider(connectionString), Utilities.RepoTypes.Standart);
                    break;
                case Utilities.ServerTypes.MySql:
                    break;
                case Utilities.ServerTypes.MongoDB:
                    connectionString = Environment.GetEnvironmentVariable("ADLE_MONGO_CONNECTION")
                        ?? @"mongodb://localhost:27017/";
                    unitOfWork = new UnitOfWorkFactory(new EntityFrameworkProvider(connectionString), Utilities.RepoTypes.Standart);
                    break;
                default:
                    connectionString = Environment.GetEnvironmentVariable("ADLE_MSSQL_CONNECTION")
                        ?? @"Server=localhost;Database=ADLE_Sim_2;Trusted_Connection=True";
                    unitOfWork = new UnitOfWorkFactory(new EntityFrameworkProvider(connectionString), Utilities.RepoTypes.Standart);
                    break;
            }

            return unitOfWork;
        }

        public static IUnitOfWork CreateBasicContext(IDataContext context)
        {
            //TODO:Config
            return new UnitOfWorkFactory(context, Utilities.RepoTypes.Basic);
        }

        internal void TransactionFlush()
        {
            _context.SaveAllChanges();
        }


        public IRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            //TODO:Config

            IRepository<TEntity> repo = null;
            switch (RepositoryType)
            {
                case Utilities.RepoTypes.Standart:
                    repo = new EFRepository<TEntity>(_context);
                    break;
                case Utilities.RepoTypes.Basic:
                    repo = new EFBaicRepository<TEntity>(_context);
                    break;
                default:
                    repo = new EFRepository<TEntity>(_context);
                    break;
            }
            return repo;
        }

        public int SaveChanges()
        {
            return _context.SaveAllChanges();
        }

        public ITransaction TransactionBegin()
        {
            //TODO:Config
            var transaction = new EFTransaction(this);
            return transaction;
        }

        public void TransactionEnd(ITransaction transaction)
        {
            if (transaction != null)
            {
                (transaction as IDisposable).Dispose();
                transaction = null;
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }

    }
}
