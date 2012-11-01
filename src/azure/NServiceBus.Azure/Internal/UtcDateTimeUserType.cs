using System;
using System.Data;
using System.Globalization;
using NHibernate;
using NHibernate.SqlTypes;

namespace NServiceBus.SagaPersisters.Azure.Config.Internal
{

    public class UtcDateTimeUserType : BaseImmutableUserType<DateTime>
    {
        public override object NullSafeGet(IDataReader rs, string[] names, object owner)
        {
            var value = (string) NHibernateUtil.String.NullSafeGet(rs, names[0]);
            var date = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal), DateTimeKind.Utc);
            return date;
        }

        public override void NullSafeSet(IDbCommand cmd, object value, int index)
        {
            var date = ((DateTime)value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            NHibernateUtil.String.NullSafeSet(cmd, date, index);
        }

        public override SqlType[] SqlTypes
        {
            get { return new [] { SqlTypeFactory.DateTime }; }
        }
    }
}