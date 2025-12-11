using System.Threading;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Enrichers.GlobalExecutionId.Dependencies
{
	public class GlobalExecutionIdRepository
	{
#if !NET451
		private static readonly AsyncLocal<string> _globalExecutionId = new AsyncLocal<string>();
#elif NET451
		protected const string GlobalExecutionId = "RawRabbit:GlobalExecutionId";
#endif
		
		public static string Get()
		{
#if NET451
			return CallContext.LogicalGetData(GlobalExecutionId) as string;
#else
			return _globalExecutionId?.Value;
#endif
		}

		public static void Set(string id)
		{
#if !NET451
			_globalExecutionId.Value = id;
#elif NET451
			CallContext.LogicalSetData(GlobalExecutionId, id);
#endif
		}
	}
}
