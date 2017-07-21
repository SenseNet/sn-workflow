using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.Workflow
{
    [ContentHandler]
    public class RegistrationWorkflow : WorkflowHandlerBase
    {
        public RegistrationWorkflow(Node parent) : this(parent, null) { }
		public RegistrationWorkflow(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected RegistrationWorkflow(NodeToken nt) : base(nt) { }

        public const string FULLNAME = "FullName";
        [RepositoryProperty(FULLNAME, RepositoryDataType.String)]
        public string FullName
        {
            get { return (string)base.GetProperty(FULLNAME); }
            set { base.SetProperty(FULLNAME, value); }
        }

        public const string USERNAME = "UserName";
        [RepositoryProperty(USERNAME, RepositoryDataType.String)]
        public string UserName
        {
            get { return (string)base.GetProperty(USERNAME); }
            set { base.SetProperty(USERNAME, value); }
        }

        public const string EMAIL = "Email";
        [RepositoryProperty(EMAIL, RepositoryDataType.String)]
        public string Email
        {
            get { return (string)base.GetProperty(EMAIL); }
            set { base.SetProperty(EMAIL, value); }
        }

        public const string PASSWORDHASH = "PasswordHash";
        [RepositoryProperty(PASSWORDHASH, RepositoryDataType.String)]
        public string PasswordHash
        {
            get { return (string)base.GetProperty(PASSWORDHASH); }
            set { base.SetProperty(PASSWORDHASH, value); }
        }

        public const string REGISTRATIONTYPE = "RegistrationType";
        [RepositoryProperty(REGISTRATIONTYPE, RepositoryDataType.String)]
        public string RegistrationType
        {
            get { return (string)base.GetProperty(REGISTRATIONTYPE); }
            set { base.SetProperty(REGISTRATIONTYPE, value); }
        }

        public const string INITIALPASSWORD = "InitialPassword";
        private string _initialPassword;
        public string InitialPassword { get { return null; } set { _initialPassword = value; } }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case FULLNAME: return FullName;
                case USERNAME: return UserName;
                case EMAIL: return Email;
                case PASSWORDHASH: return PasswordHash;
                case REGISTRATIONTYPE: return RegistrationType;
                case INITIALPASSWORD: return InitialPassword;
                default: return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case FULLNAME: FullName = (string)value; break;
                case USERNAME: UserName = (string)value; break;
                case EMAIL: Email = (string)value; break;
                case PASSWORDHASH: PasswordHash = (string)value; break;
                case REGISTRATIONTYPE: RegistrationType = (string)value; break;
                case INITIALPASSWORD: InitialPassword = (string)value; break;
                default: base.SetProperty(name, value); break;
            }
        }

        public override void Save(NodeSaveSettings settings)
        {
            AssertUserName();

            if (!string.IsNullOrEmpty(_initialPassword))
                this.PasswordHash = ContentRepository.Fields.PasswordField.EncodePassword(_initialPassword, this);

            base.Save(settings);
        }


        private void AssertUserName()
        {
            if (!RepositoryInstance.ContentQueryIsAllowed)
                return;

            if(this.WorkflowStarted)
                return;

            var uname = this.UserName;
            if (string.IsNullOrEmpty(uname))
                throw new InvalidOperationException(String.Format(SenseNetResourceManager.Current.GetString("RegistrationWorkflow", "CompulsoryField"), USERNAME));

            if (ContentQuery.Query(SafeQueries.UserCountByName, null, uname).Count > 0)
                throw new InvalidOperationException(String.Format(SenseNetResourceManager.Current.GetString("RegistrationWorkflow", "UserNameAlreadyExist"), uname));
        }
    }
}
