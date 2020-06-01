using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace QuartzAdmin
{
    public class SchedulerFacade : IScheduler
    {
        public static IScheduler Scheduler { get; set; }

        string IScheduler.SchedulerName => Scheduler.SchedulerName;

        string IScheduler.SchedulerInstanceId => Scheduler.SchedulerInstanceId;

        SchedulerContext IScheduler.Context => Scheduler.Context;

        bool IScheduler.InStandbyMode => Scheduler.InStandbyMode;

        bool IScheduler.IsShutdown => Scheduler.IsShutdown;

        IJobFactory IScheduler.JobFactory { set => Scheduler.JobFactory = value; }

        IListenerManager IScheduler.ListenerManager => Scheduler.ListenerManager;

        bool IScheduler.IsStarted => Scheduler.IsStarted;

        Task IScheduler.AddCalendar(string calName, ICalendar calendar, bool replace, bool updateTriggers, CancellationToken cancellationToken)
        {
            return Scheduler.AddCalendar(calName, calendar, replace, updateTriggers, cancellationToken);
        }

        Task IScheduler.AddJob(IJobDetail jobDetail, bool replace, CancellationToken cancellationToken)
        {
            return Scheduler.AddJob(jobDetail, replace, cancellationToken);
        }

        Task IScheduler.AddJob(IJobDetail jobDetail, bool replace, bool storeNonDurableWhileAwaitingScheduling, CancellationToken cancellationToken)
        {
            return Scheduler.AddJob(jobDetail, replace,storeNonDurableWhileAwaitingScheduling, cancellationToken);
        }

        Task<bool> IScheduler.CheckExists(JobKey jobKey, CancellationToken cancellationToken)
        {
            return Scheduler.CheckExists(jobKey, cancellationToken);
        }

        Task<bool> IScheduler.CheckExists(TriggerKey triggerKey, CancellationToken cancellationToken)
        {
            return Scheduler.CheckExists(triggerKey, cancellationToken);
        }

        Task IScheduler.Clear(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.DeleteCalendar(string calName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.DeleteJob(JobKey jobKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.DeleteJobs(IReadOnlyCollection<JobKey> jobKeys, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<ICalendar> IScheduler.GetCalendar(string calName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<string>> IScheduler.GetCalendarNames(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<IJobExecutionContext>> IScheduler.GetCurrentlyExecutingJobs(CancellationToken cancellationToken)
        {
            return Scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        }

        Task<IJobDetail> IScheduler.GetJobDetail(JobKey jobKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<string>> IScheduler.GetJobGroupNames(CancellationToken cancellationToken)
        {
            return Scheduler.GetJobGroupNames(cancellationToken);

        }

        Task<IReadOnlyCollection<JobKey>> IScheduler.GetJobKeys(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken)
        {
            return Scheduler.GetJobKeys(matcher, cancellationToken);

        }

        Task<SchedulerMetaData> IScheduler.GetMetaData(CancellationToken cancellationToken)
        {
            return Scheduler.GetMetaData(cancellationToken);
        }

        Task<IReadOnlyCollection<string>> IScheduler.GetPausedTriggerGroups(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<ITrigger> IScheduler.GetTrigger(TriggerKey triggerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<string>> IScheduler.GetTriggerGroupNames(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<TriggerKey>> IScheduler.GetTriggerKeys(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<ITrigger>> IScheduler.GetTriggersOfJob(JobKey jobKey, CancellationToken cancellationToken)
        {
           return Scheduler.GetTriggersOfJob(jobKey, cancellationToken);
         //   throw new NotImplementedException();
        }

        Task<TriggerState> IScheduler.GetTriggerState(TriggerKey triggerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.Interrupt(JobKey jobKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.Interrupt(string fireInstanceId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.IsJobGroupPaused(string groupName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.IsTriggerGroupPaused(string groupName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.PauseAll(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.PauseJob(JobKey jobKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.PauseJobs(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.PauseTrigger(TriggerKey triggerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.PauseTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DateTimeOffset?> IScheduler.RescheduleJob(TriggerKey triggerKey, ITrigger newTrigger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.ResumeAll(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.ResumeJob(JobKey jobKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.ResumeJobs(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.ResumeTrigger(TriggerKey triggerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.ResumeTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DateTimeOffset> IScheduler.ScheduleJob(IJobDetail jobDetail, ITrigger trigger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DateTimeOffset> IScheduler.ScheduleJob(ITrigger trigger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.ScheduleJob(IJobDetail jobDetail, IReadOnlyCollection<ITrigger> triggersForJob, bool replace, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.ScheduleJobs(IReadOnlyDictionary<IJobDetail, IReadOnlyCollection<ITrigger>> triggersAndJobs, bool replace, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.Shutdown(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.Shutdown(bool waitForJobsToComplete, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.Standby(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.Start(CancellationToken cancellationToken)
        {
            return Scheduler.Start(cancellationToken);
        }

        Task IScheduler.StartDelayed(TimeSpan delay, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.TriggerJob(JobKey jobKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IScheduler.TriggerJob(JobKey jobKey, JobDataMap data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.UnscheduleJob(TriggerKey triggerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IScheduler.UnscheduleJobs(IReadOnlyCollection<TriggerKey> triggerKeys, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}