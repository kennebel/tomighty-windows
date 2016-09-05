﻿//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Tomighty.Events;

namespace Tomighty.Test
{
    [TestFixture]
    public class PomodoroEngineTest
    {
        private IPomodoroEngine engine;
        private IUserPreferences userPreferences;
        private ITimer timer;
        private FakeEventHub eventHub;

        [SetUp]
        public void SetUp()
        {
            userPreferences = Substitute.For<IUserPreferences>();
            timer = Substitute.For<ITimer>();
            eventHub = new FakeEventHub();
            engine = new PomodoroEngine(timer, userPreferences, eventHub);
        }

        [Test]
        public void Initial_pomodoro_count_is_zero()
        {
            Assert.AreEqual(0, engine.PomodoroCount);
        }

        [Test]
        public void Start_the_timer()
        {
            var duration = Duration.InMinutes(25);
            var intervalType = IntervalType.Pomodoro;

            userPreferences.GetIntervalDuration(intervalType).Returns(duration);

            engine.StartTimer(intervalType);

            timer.Received().Start(duration, intervalType);
        }

        [Test]
        public void Stop_the_timer()
        {
            engine.StopTimer();
            timer.Received().Stop();
        }

        [Test]
        public void Publish_PomodoroCompleted_event_when_timer_stops_with_zero_remaining_time()
        {
            var remainingTime = Duration.Zero;
            var duration = Duration.InMinutes(25);

            eventHub.Publish(new TimerStopped(IntervalType.Pomodoro, duration, remainingTime));

            var pomodoroCompleted = eventHub.PublishedEvents<PomodoroCompleted>().Single();
            Assert.AreEqual(duration, pomodoroCompleted.Duration);
        }

        [Test]
        public void Do_not_publish_PomodoroCompleted_event_when_timer_stops_with_remaining_time_greater_than_zero()
        {
            var remainingTime = Duration.InSeconds(1);
            var duration = Duration.InMinutes(25);

            eventHub.Publish(new TimerStopped(IntervalType.Pomodoro, duration, remainingTime));

            var pomodoroCompletedEvents = eventHub.PublishedEvents<PomodoroCompleted>();
            Assert.AreEqual(0, pomodoroCompletedEvents.Count());
        }

        [Test]
        public void Do_not_publish_PomodoroCompleted_event_when_timer_stops_and_interval_type_is_not_Pomodoro()
        {
            var remainingTime = Duration.Zero;
            var duration = Duration.InMinutes(25);

            eventHub.Publish(new TimerStopped(IntervalType.ShortBreak, duration, remainingTime));

            var pomodoroCompletedEvents = eventHub.PublishedEvents<PomodoroCompleted>();
            Assert.AreEqual(0, pomodoroCompletedEvents.Count());
        }

        [Test]
        public void Increase_pomodoro_count_when_timer_stops_with_zero_remaining_time()
        {
            var remainingTime = Duration.Zero;
            var duration = Duration.InMinutes(25);
            var timerStopped = new TimerStopped(IntervalType.Pomodoro, duration, remainingTime);

            userPreferences.MaxPomodoroCount.Returns(999);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(1, engine.PomodoroCount);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(2, engine.PomodoroCount);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(3, engine.PomodoroCount);
        }

        [Test]
        public void Reset_pomodoro_count_when_it_is_greater_than_limit_defined_by_user_preferences()
        {
            var remainingTime = Duration.Zero;
            var duration = Duration.InMinutes(25);
            var timerStopped = new TimerStopped(IntervalType.Pomodoro, duration, remainingTime);

            userPreferences.MaxPomodoroCount.Returns(3);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(1, engine.PomodoroCount);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(2, engine.PomodoroCount);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(3, engine.PomodoroCount);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(1, engine.PomodoroCount);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(2, engine.PomodoroCount);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(3, engine.PomodoroCount);

            eventHub.Publish(timerStopped);
            Assert.AreEqual(1, engine.PomodoroCount);
        }

        [Test]
        public void Suggested_timer_action_is_ShortBreak_when_pomodoro_count_is_different_than_user_defined_count_limit()
        {
            userPreferences.MaxPomodoroCount.Returns(3);

            var remainingTime = Duration.Zero;
            var duration = Duration.InMinutes(25);
            var timerStopped = new TimerStopped(IntervalType.Pomodoro, duration, remainingTime);

            // pomodoro count: 0
            Assert.AreEqual(IntervalType.ShortBreak, engine.SuggestedBreakType);

            // pomodoro count: 1
            eventHub.Publish(timerStopped);
            Assert.AreEqual(IntervalType.ShortBreak, engine.SuggestedBreakType);

            // pomodoro count: 2
            eventHub.Publish(timerStopped);
            Assert.AreEqual(IntervalType.ShortBreak, engine.SuggestedBreakType);
        }

        [Test]
        public void Suggested_timer_action_is_LongBreak_when_pomodoro_count_is_equal_to_user_defined_count_limit()
        {
            userPreferences.MaxPomodoroCount.Returns(3);

            var remainingTime = Duration.Zero;
            var duration = Duration.InMinutes(25);
            var timerStopped = new TimerStopped(IntervalType.Pomodoro, duration, remainingTime);

            // pomodoro count: 1
            eventHub.Publish(timerStopped);
            
            // pomodoro count: 2
            eventHub.Publish(timerStopped);
            
            // pomodoro count: 3
            eventHub.Publish(timerStopped);

            Assert.AreEqual(IntervalType.LongBreak, engine.SuggestedBreakType);
        }
    }
}
