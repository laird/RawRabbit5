using System;
using Ninject;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Ninject
{
	[Obsolete("Ninject DI adapter is deprecated. Use Microsoft.Extensions.DependencyInjection instead (RawRabbit.DependencyInjection.ServiceCollection). This package will be removed in a future version.")]
	public static class KernelExtension
	{
		[Obsolete("Ninject DI adapter is deprecated. Use Microsoft.Extensions.DependencyInjection instead (RawRabbit.DependencyInjection.ServiceCollection). This package will be removed in a future version.")]
		public static IKernel RegisterRawRabbit(this IKernel kernel, RawRabbitOptions? options = null)
		{
			if (options != null)
			{
				kernel.Bind<RawRabbitOptions>().ToConstant(options);
			}
			kernel.Load<RawRabbitModule>();
			return kernel;
		}
	}
}
