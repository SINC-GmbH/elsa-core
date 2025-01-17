using Elsa.Activities.Kafka.Activities.KafkaMessageReceived;
using Elsa.Activities.Kafka.Activities.SendKafkaMessage;
using Elsa.Activities.Kafka.Bookmarks;
using Elsa.Activities.Kafka.Configuration;
using Elsa.Activities.Kafka.Consumers;
using Elsa.Activities.Kafka.Services;
using Elsa.Activities.Kafka.StartupTasks;
using Elsa.Events;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Elsa.Activities.Kafka.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddKafkaActivities(this ElsaOptionsBuilder options)
        {
            options.Services
                .AddSingleton<BusClientFactory>()
                .AddSingleton<IMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IMessageSenderClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IWorkerManager, WorkerManager>()
                .AddHostedService<StartKafkaQueues>()
                .AddSingleton<IKafkaTenantIdResolver, DefaultKafkaTenantIdResolver>()
                .AddSingleton<IKafkaCustomActivityProvider, KafkaCustomActivityProvider>()
                .AddBookmarkProvider<QueueMessageReceivedBookmarkProvider>();

            options.AddPubSubConsumer<UpdateWorkers, TriggerIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, TriggersDeleted>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, BookmarkIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, BookmarksDeleted>("WorkflowManagementEvents");


            options
                .AddActivity<KafkaMessageReceived>()
                .AddActivity<SendKafkaMessage>();

            return options;
        }
        public static ElsaOptionsBuilder AddCustomKafkaActivitiesFrom(this ElsaOptionsBuilder options, params Type[] assemblyMarkerTypes) => AddCustomKafkaActivitiesFrom(options, assemblyMarkerTypes.Select(x => x.Assembly).Distinct());

        public static ElsaOptionsBuilder AddCustomKafkaActivitiesFrom<TMarker>(this ElsaOptionsBuilder options) where TMarker : class => AddCustomKafkaActivitiesFrom(options, typeof(TMarker));

        public static ElsaOptionsBuilder AddCustomKafkaActivitiesFrom(this ElsaOptionsBuilder options, IEnumerable<Assembly> assemblies)
        {
            var triggers = assemblies.SelectMany(x => x.GetAllWithBaseClass(typeof(KafkaMessageReceived)));

           options.Services.
                AddSingleton<IKafkaCustomActivityProvider>(new KafkaCustomActivityProvider() { KafkaOverrideTriggers = triggers.Select(x => x.Name).ToList() });

            return options;
        }

    }
}