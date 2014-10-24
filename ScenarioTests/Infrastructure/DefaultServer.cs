namespace ScenarioTests.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus.Logging;
    using NServiceBus;
    using NServiceBus.Persistence;
    using NServiceBus.AcceptanceTesting;

    /// <summary>
    /// Serves as a template for the NServiceBus configuration of an endpoint.
    /// You can do all sorts of fancy stuff here, such as support multiple transports, etc.
    /// Here, I stripped it down to support just the defaults (MSMQ transport).
    /// </summary>
    public class DefaultServer : IEndpointSetupTemplate
    {
        public BusConfiguration GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource, Action<BusConfiguration> configurationBuilderCustomization)
        {
            var settings = runDescriptor.Settings;

            var types = GetTypesToUse(endpointConfiguration);

            var config = new BusConfiguration();
            config.EndpointName(endpointConfiguration.EndpointName);
            config.TypesToScan(types);
            config.CustomConfigurationSource(configSource);
            config.UsePersistence<InMemoryPersistence>();
            config.PurgeOnStartup(true);

            // Plugin a behavior that listens for subscription messages
            config.Pipeline.Register<SubscriptionBehavior.Registration>();
            config.RegisterComponents(c => c.ConfigureComponent<SubscriptionBehavior>(DependencyLifecycle.InstancePerCall));
            
            // Important: you need to make sure that the correct ScenarioContext class is available to your endpoints and tests
            config.RegisterComponents(r =>
            {
                r.RegisterSingleton(runDescriptor.ScenarioContext.GetType(), runDescriptor.ScenarioContext);
                r.RegisterSingleton(typeof(ScenarioContext), runDescriptor.ScenarioContext);
            });

            // Call extra custom action if provided
            if (configurationBuilderCustomization != null)
            {
                configurationBuilderCustomization(config);
            }

            return config;
        }

        static IEnumerable<Type> GetTypesToUse(EndpointConfiguration endpointConfiguration)
        {
            var assemblies = new AssemblyScanner().GetScannableAssemblies();

            var types = assemblies.Assemblies
                //exclude all test types by default
                                  .Where(a => a != Assembly.GetExecutingAssembly())
                                  .SelectMany(a => a.GetTypes());


            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType, endpointConfiguration.BuilderType));

            types = types.Union(endpointConfiguration.TypesToInclude);

            return types.Where(t => !endpointConfiguration.TypesToExclude.Contains(t)).ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
                yield break;

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }
    }
}