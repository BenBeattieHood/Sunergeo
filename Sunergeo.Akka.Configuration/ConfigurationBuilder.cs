﻿using System;

namespace Sunergeo.Akka.Configuration
{
    public static class ConfigurationBuilder
    {
        public static string Create(
            Type defaultSerializer,
            Type byteArraySerializer
            )
        {
            // from https://github.com/akkadotnet/akka.net/blob/dev/src/core/Akka/Configuration/Pigeon.conf

            var config = $@"####################################
# Akka Actor Reference Config File #
####################################
 
# This is the reference config file that contains all the default settings.
# Make your edits/overrides in your application.conf.
 
akka {{
  # Akka version, checked against the runtime version of Akka.
  version = ""0.0.1 Akka""
 
  # Home directory of Akka, modules in the deploy directory will be loaded
  home = """"
 
  # Loggers to register at boot time (akka.event.Logging$DefaultLogger logs
  # to STDOUT)
  loggers = [""Akka.Event.DefaultLogger""]

  # Specifies the default loggers dispatcher
  loggers-dispatcher = ""akka.actor.default-dispatcher""
 
  # Loggers are created and registered synchronously during ActorSystem
  # start-up, and since they are actors, this timeout is used to bound the
  # waiting time
  logger-startup-timeout = 5s
 
  # Log level used by the configured loggers (see ""loggers"") as soon
  # as they have been started; before that, see ""stdout-loglevel""
  # Options: OFF, ERROR, WARNING, INFO, DEBUG
  loglevel = ""INFO""

  # Suppresses warning about usage of the default (JSON.NET) serializer
  # which is going to be obsoleted at v1.5
  suppress-json-serializer-warning = off
 
  # Log level for the very basic logger activated during AkkaApplication startup
  # Options: OFF, ERROR, WARNING, INFO, DEBUG
  stdout-loglevel = ""WARNING""
 
  # Log the complete configuration at INFO level when the actor system is started.
  # This is useful when you are uncertain of what configuration is used.
  log-config-on-start = off
 
  # Log at info level when messages are sent to dead letters.
  # Possible values:
  # on: all dead letters are logged
  # off: no logging of dead letters
  # n: positive integer, number of dead letters that will be logged
  log-dead-letters = 10
 
  # Possibility to turn off logging of dead letters while the actor system
  # is shutting down. Logging is only done when enabled by 'log-dead-letters'
  # setting.
  log-dead-letters-during-shutdown = on
 
  # List FQCN of extensions which shall be loaded at actor system startup.
  # Should be on the format: 'extensions = [""foo"", ""bar""]' etc.
  # See the Akka Documentation for more info about Extensions
  extensions = []
 
  # Toggles whether threads created by this ActorSystem should be daemons or not
  daemonic = off
 
  # THIS DOES NOT APPLY TO .NET
  #
  # JVM shutdown, System.exit(-1), in case of a fatal error,
  # such as OutOfMemoryError
  #jvm-exit-on-fatal-error = on
 
  actor {{
 
    # FQCN of the ActorRefProvider to be used; the below is the built-in default,
    # another one is akka.remote.RemoteActorRefProvider in the akka-remote bundle.
    provider = ""Akka.Actor.LocalActorRefProvider""
 
    # The guardian ""/user"" will use this class to obtain its supervisorStrategy.
    # It needs to be a subclass of Akka.Actor.SupervisorStrategyConfigurator.
    # In addition to the default there is Akka.Actor.StoppingSupervisorStrategy.
    guardian-supervisor-strategy = ""Akka.Actor.DefaultSupervisorStrategy""
 
    # Timeout for ActorSystem.actorOf
    creation-timeout = 20s
 
    # Frequency with which stopping actors are prodded in case they had to be
    # removed from their parents
    reaper-interval = 5
 
    # Serializes and deserializes (non-primitive) messages to ensure immutability,
    # this is only intended for testing.
    serialize-messages = off
 
    # Serializes and deserializes creators (in Props) to ensure that they can be
    # sent over the network, this is only intended for testing. Purely local deployments
    # as marked with deploy.scope == LocalScope are exempt from verification.
    serialize-creators = off
 
    # Timeout for send operations to top-level actors which are in the process
    # of being started. This is only relevant if using a bounded mailbox or the
    # CallingThreadDispatcher for a top-level actor.
    unstarted-push-timeout = 10s

    # Default timeout for IActorRef.Ask.
    ask-timeout = infinite
 

    # THIS DOES NOT APPLY TO .NET
    #
    typed {{
      # Default timeout for typed actor methods with non-void return type
      timeout = 5
    }}

    inbox {{
        inbox-size = 1000,
        default-timeout = 5s
    }}
    
    # Mapping between ´deployment.router' short names to fully qualified class names
    router.type-mapping {{
          from-code = ""Akka.Routing.NoRouter""
          round-robin-pool = ""Akka.Routing.RoundRobinPool""
          round-robin-group = ""Akka.Routing.RoundRobinGroup""
          random-pool = ""Akka.Routing.RandomPool""
          random-group = ""Akka.Routing.RandomGroup""
          balancing-pool = ""Akka.Routing.BalancingPool""
          smallest-mailbox-pool = ""Akka.Routing.SmallestMailboxPool""
          broadcast-pool = ""Akka.Routing.BroadcastPool""
          broadcast-group = ""Akka.Routing.BroadcastGroup""
          scatter-gather-pool = ""Akka.Routing.ScatterGatherFirstCompletedPool""
          scatter-gather-group = ""Akka.Routing.ScatterGatherFirstCompletedGroup""
          consistent-hashing-pool = ""Akka.Routing.ConsistentHashingPool""
          consistent-hashing-group = ""Akka.Routing.ConsistentHashingGroup""
    }}
 
    deployment {{
 
      # deployment id pattern - on the format: /parent/child etc.
      default {{
      
        # The id of the dispatcher to use for this actor.
        # If undefined or empty the dispatcher specified in code
        # (Props.withDispatcher) is used, or default-dispatcher if not
        # specified at all.
        dispatcher = """"
 
        # The id of the mailbox to use for this actor.
        # If undefined or empty the default mailbox of the configured dispatcher
        # is used or if there is no mailbox configuration the mailbox specified
        # in code (Props.withMailbox) is used.
        # If there is a mailbox defined in the configured dispatcher then that
        # overrides this setting.
        mailbox = """"
 
        # routing (load-balance) scheme to use
        # - available: ""from-code"", ""round-robin"", ""random"", ""smallest-mailbox"",
        #              ""scatter-gather"", ""broadcast""
        # - or:        Fully qualified class name of the router class.
        #              The class must extend akka.routing.CustomRouterConfig and
        #              have a public constructor with com.typesafe.config.Config
        #              and optional akka.actor.DynamicAccess parameter.
        # - default is ""from-code"";
        # Whether or not an actor is transformed to a Router is decided in code
        # only (Props.withRouter). The type of router can be overridden in the
        # configuration; specifying ""from-code"" means that the values specified
        # in the code shall be used.
        # In case of routing, the actors to be routed to can be specified
        # in several ways:
        # - nr-of-instances: will create that many children
        # - routees.paths: will route messages to these paths using ActorSelection,
        #   i.e. will not create children
        # - resizer: dynamically resizable number of routees as specified in
        #   resizer below
        router = ""from-code""
 
        # number of children to create in case of a router;
        # this setting is ignored if routees.paths is given
        nr-of-instances = 1
 
        # within is the timeout used for routers containing future calls
        within = 5 s
 
        # number of virtual nodes per node for consistent-hashing router
        virtual-nodes-factor = 10
 
        routees {{
          # Alternatively to giving nr-of-instances you can specify the full
          # paths of those actors which should be routed to. This setting takes
          # precedence over nr-of-instances
          paths = []
        }}
        
        # To use a dedicated dispatcher for the routees of the pool you can
        # define the dispatcher configuration inline with the property name 
        # 'pool-dispatcher' in the deployment section of the router.
        # For example:
        # pool-dispatcher {{
        #   fork-join-executor.parallelism-min = 5
        #   fork-join-executor.parallelism-max = 5
        # }}
 
        # Routers with dynamically resizable number of routees; this feature is
        # enabled by including (parts of) this section in the deployment
        resizer {{
        
          enabled = off
 
          # The fewest number of routees the router should ever have.
          lower-bound = 1
 
          # The most number of routees the router should ever have.
          # Must be greater than or equal to lower-bound.
          upper-bound = 10
 
          # Threshold used to evaluate if a routee is considered to be busy
          # (under pressure). Implementation depends on this value (default is 1).
          # 0:   number of routees currently processing a message.
          # 1:   number of routees currently processing a message has
          #      some messages in mailbox.
          # > 1: number of routees with at least the configured pressure-threshold
          #      messages in their mailbox. Note that estimating mailbox size of
          #      default UnboundedMailbox is O(N) operation.
          pressure-threshold = 1
 
          # Percentage to increase capacity whenever all routees are busy.
          # For example, 0.2 would increase 20% (rounded up), i.e. if current
          # capacity is 6 it will request an increase of 2 more routees.
          rampup-rate = 0.2
 
          # Minimum fraction of busy routees before backing off.
          # For example, if this is 0.3, then we'll remove some routees only when
          # less than 30% of routees are busy, i.e. if current capacity is 10 and
          # 3 are busy then the capacity is unchanged, but if 2 or less are busy
          # the capacity is decreased.
          # Use 0.0 or negative to avoid removal of routees.
          backoff-threshold = 0.3
 
          # Fraction of routees to be removed when the resizer reaches the
          # backoffThreshold.
          # For example, 0.1 would decrease 10% (rounded up), i.e. if current
          # capacity is 9 it will request an decrease of 1 routee.
          backoff-rate = 0.1
 
          # Number of messages between resize operation.
          # Use 1 to resize before each message.
          messages-per-resize = 10
        }}
      }}
    }}

    #used for GUI applications
    synchronized-dispatcher {{
        type = ""SynchronizedDispatcher""
		executor = ""current-context-executor""
        throughput = 10
    }}

    task-dispatcher {{
        type = ""TaskDispatcher""
		executor = ""task-executor""
        throughput = 30
    }}

    default-fork-join-dispatcher{{
        type = ForkJoinDispatcher
		executor = fork-join-executor
        throughput = 30
        dedicated-thread-pool{{ #settings for Helios.DedicatedThreadPool
            thread-count = 3 #number of threads
            #deadlock-timeout = 3s #optional timeout for deadlock detection
            threadtype = background #values can be ""background"" or ""foreground""
        }}
    }}
    
    default-dispatcher {{
      # Must be one of the following
      # Dispatcher, PinnedDispatcher, or a FQCN to a class inheriting
      # MessageDispatcherConfigurator with a public constructor with
      # both com.typesafe.config.Config parameter and
      # akka.dispatch.DispatcherPrerequisites parameters.
      # PinnedDispatcher must be used together with executor=fork-join-executor.
      type = ""Dispatcher""

	  # Which kind of ExecutorService to use for this dispatcher
      # Valid options:
      #  - ""default-executor"" requires a ""default-executor"" section
      #  - ""fork-join-executor"" requires a ""fork-join-executor"" section
      #  - ""thread-pool-executor"" requires a ""thread-pool-executor"" section
	  #  - ""current-context-executor"" requires a ""current-context-executor"" section
	  #  - ""task-executor"" requires a ""task-executor"" section
      #  - A FQCN of a class extending ExecutorServiceConfigurator
      executor = ""default-executor""

	  # This will be used if you have set ""executor = ""default-executor"""".
	  # Uses the default .NET threadpool
      default-executor {{
        
      }}

	  # Same as default executor
	  thread-pool-executor{{
	  }}

	  # This will be used if you have set ""executor = ""fork-join-executor""""
      # Underlying thread pool implementation is scala.concurrent.forkjoin.ForkJoinPool
      fork-join-executor {{
       dedicated-thread-pool{{ #settings for Helios.DedicatedThreadPool
            thread-count = 3 #number of threads
            #deadlock-timeout = 3s #optional timeout for deadlock detection
            threadtype = background #values can be ""background"" or ""foreground""
        }}
      }}

	  # For running in current synchronization contexts
	  current-context-executor{{}}
      
      # How long time the dispatcher will wait for new actors until it shuts down
      shutdown-timeout = 1s
 
      # Throughput defines the number of messages that are processed in a batch
      # before the thread is returned to the pool. Set to 1 for as fair as possible.
      throughput = 30
 
      # Throughput deadline for Dispatcher, set to 0 or negative for no deadline
      throughput-deadline-time = 0ms
 
      # For BalancingDispatcher: If the balancing dispatcher should attempt to
      # schedule idle actors using the same dispatcher when a message comes in,
      # and the dispatchers ExecutorService is not fully busy already.
      attempt-teamwork = on
 
      # If this dispatcher requires a specific type of mailbox, specify the
      # fully-qualified class name here; the actually created mailbox will
      # be a subtype of this type. The empty string signifies no requirement.
      mailbox-requirement = """"
    }}
 
    default-mailbox {{
      # FQCN of the MailboxType. The Class of the FQCN must have a public
      # constructor with
      # (akka.actor.ActorSystem.Settings, com.typesafe.config.Config) parameters.
      mailbox-type = ""Akka.Dispatch.UnboundedMailbox""
 
      # If the mailbox is bounded then it uses this setting to determine its
      # capacity. The provided value must be positive.
      # NOTICE:
      # Up to version 2.1 the mailbox type was determined based on this setting;
      # this is no longer the case, the type must explicitly be a bounded mailbox.
      mailbox-capacity = 1000
 
      # If the mailbox is bounded then this is the timeout for enqueueing
      # in case the mailbox is full. Negative values signify infinite
      # timeout, which should be avoided as it bears the risk of dead-lock.
      mailbox-push-timeout-time = 10s
 
      # For Actor with Stash: The default capacity of the stash.
      # If negative (or zero) then an unbounded stash is used (default)
      # If positive then a bounded stash is used and the capacity is set using
      # the property
      stash-capacity = -1
    }}
 
    mailbox {{
      # Mapping between message queue semantics and mailbox configurations.
      # Used by akka.dispatch.RequiresMessageQueue[T] to enforce different
      # mailbox types on actors.
      # If your Actor implements RequiresMessageQueue[T], then when you create
      # an instance of that actor its mailbox type will be decided by looking
      # up a mailbox configuration via T in this mapping
      requirements {{
        ""Akka.Dispatch.IUnboundedMessageQueueSemantics"" = akka.actor.mailbox.unbounded-queue-based
        ""Akka.Dispatch.IBoundedMessageQueueSemantics"" = akka.actor.mailbox.bounded-queue-based
        ""Akka.Dispatch.IDequeBasedMessageQueueSemantics"" = akka.actor.mailbox.unbounded-deque-based
        ""Akka.Dispatch.IUnboundedDequeBasedMessageQueueSemantics"" = akka.actor.mailbox.unbounded-deque-based
        ""Akka.Dispatch.IBoundedDequeBasedMessageQueueSemantics"" = akka.actor.mailbox.bounded-deque-based
        ""Akka.Dispatch.IMultipleConsumerSemantics"" = akka.actor.mailbox.unbounded-queue-based
        ""Akka.Event.ILoggerMessageQueueSemantics"" = akka.actor.mailbox.logger-queue
      }}
 
      unbounded-queue-based {{
        # FQCN of the MailboxType, The Class of the FQCN must have a public
        # constructor with (akka.actor.ActorSystem.Settings,
        # com.typesafe.config.Config) parameters.
        mailbox-type = ""Akka.Dispatch.UnboundedMailbox""
      }}
 
      bounded-queue-based {{
        # FQCN of the MailboxType, The Class of the FQCN must have a public
        # constructor with (akka.actor.ActorSystem.Settings,
        # com.typesafe.config.Config) parameters.
        mailbox-type = ""Akka.Dispatch.BoundedMailbox""
      }}
 
      unbounded-deque-based {{
        # FQCN of the MailboxType, The Class of the FQCN must have a public
        # constructor with (akka.actor.ActorSystem.Settings,
        # com.typesafe.config.Config) parameters.
        mailbox-type = ""Akka.Dispatch.UnboundedDequeBasedMailbox""
      }}
 
      bounded-deque-based {{
        # FQCN of the MailboxType, The Class of the FQCN must have a public
        # constructor with (akka.actor.ActorSystem.Settings,
        # com.typesafe.config.Config) parameters.
        mailbox-type = ""Akka.Dispatch.BoundedDequeBasedMailbox""
      }}

      # The LoggerMailbox will drain all messages in the mailbox
      # when the system is shutdown and deliver them to the StandardOutLogger.
      # Do not change this unless you know what you are doing.
      logger-queue {{
        mailbox-type = ""Akka.Event.LoggerMailboxType""
      }}
    }}
 
    debug {{
      # enable function of Actor.loggable(), which is to log any received message
      # at DEBUG level, see the “Testing Actor Systems” section of the Akka
      # Documentation at http://akka.io/docs
      receive = off
 
      # enable DEBUG logging of all AutoReceiveMessages (Kill, PoisonPill et.c.)
      autoreceive = off
 
      # enable DEBUG logging of actor lifecycle changes
      lifecycle = off
 
      # enable DEBUG logging of all LoggingFSMs for events, transitions and timers
      fsm = off
 
      # enable DEBUG logging of subscription changes on the eventStream
      event-stream = off
 
      # enable DEBUG logging of unhandled messages
      unhandled = off
 
      # enable WARN logging of misconfigured routers
      router-misconfiguration = off
    }}
 
    # Entries for pluggable serializers and their bindings.

    serializers {{
      object = ""{defaultSerializer.FullName}, {defaultSerializer.Assembly.GetName().Name}""
      bytes = ""{byteArraySerializer.FullName}, {byteArraySerializer.Assembly.GetName().Name}""
    }}
 
    # Class to Serializer binding. You only need to specify the name of an
    # interface or abstract base class of the messages. In case of ambiguity it
    # is using the most specific configured class, or giving a warning and
    # choosing the “first” one.
    #
    # To disable one of the default serializers, assign its class to ""none"", like
    # ""java.io.Serializable"" = none
    serialization-bindings {{
      ""System.Byte[]"" = bytes
      ""System.Object"" = json
    }}

    # Configuration namespace of serialization identifiers.
    # Each serializer implementation must have an entry in the following format:
    # `akka.actor.serialization-identifiers.""FQCN"" = ID`
    # where `FQCN` is fully qualified class name of the serializer implementation
    # and `ID` is globally unique serializer identifier number.
    # Identifier values from 0 to 40 are reserved for Akka internal usage.
    serialization-identifiers {{
      ""{byteArraySerializer.FullName}, {byteArraySerializer.Assembly.GetName().Name}"" = 4
      ""{defaultSerializer.FullName}, {defaultSerializer.Assembly.GetName().Name}"" = 1
    }}
	
	# extra settings that can be custom to a serializer implementation
	serialization-settings {{
	
	}}
  }}
 
  # Used to set the behavior of the scheduler.
  # Changing the default values may change the system behavior drastically so make
  # sure you know what you're doing! See the Scheduler section of the Akka
  # Documentation for more details.
  scheduler {{
    # The LightArrayRevolverScheduler is used as the default scheduler in the
    # system. It does not execute the scheduled tasks on exact time, but on every
    # tick, it will run everything that is (over)due. You can increase or decrease
    # the accuracy of the execution timing by specifying smaller or larger tick
    # duration. If you are scheduling a lot of tasks you should consider increasing
    # the ticks per wheel.
    # Note that it might take up to 1 tick to stop the Timer, so setting the
    # tick-duration to a high value will make shutting down the actor system
    # take longer.
    tick-duration = 10ms
 
    # The timer uses a circular wheel of buckets to store the timer tasks.
    # This should be set such that the majority of scheduled timeouts (for high
    # scheduling frequency) will be shorter than one rotation of the wheel
    # (ticks-per-wheel * ticks-duration)
    # THIS MUST BE A POWER OF TWO!
    ticks-per-wheel = 512
 
    # This setting selects the timer implementation which shall be loaded at
    # system start-up.
    # The class given here must implement the akka.actor.Scheduler interface
    # and offer a public constructor which takes three arguments:
    #  1) com.typesafe.config.Config
    #  2) akka.event.LoggingAdapter
    #  3) java.util.concurrent.ThreadFactory
    implementation = ""Akka.Actor.HashedWheelTimerScheduler""
 
    # When shutting down the scheduler, there will typically be a thread which
    # needs to be stopped, and this timeout determines how long to wait for
    # that to happen. In case of timeout the shutdown of the actor system will
    # proceed without running possibly still enqueued tasks.
    shutdown-timeout = 5s
  }}   
  
  io {{

    # By default the select loops run on dedicated threads, hence using a
    # PinnedDispatcher
    pinned-dispatcher {{
      type = ""PinnedDispatcher""
      executor = ""fork-join-executor""
    }}

    tcp {{

      # Default implementation of `Akka.IO.Buffers.IBufferPool` interface. It
      # allocates memory is so called segments. Each segment is then cut into 
      # buffers of equal size (see: `buffers-per-segment`). Those buffers are 
      # then lend to the requestor. They have to be released later on.
      direct-buffer-pool {{

        # Class implementing `Akka.IO.Buffers.IBufferPool` interface, which
        # will be created with this configuration.
        class = ""Akka.IO.Buffers.DirectBufferPool, Akka""

        # Size of a single byte buffer in bytes.
        buffer-size = 256

        # Number of byte buffers per segment. Every segement is a single continuous
        # byte array in memory. Once buffer pool will run out of byte buffers to 
        # lend it will allocate a next segment of memory. 
        # Each segments size is equal to `buffer-size` * `buffers-per-segment`.
        buffers-per-segment = 500

        # Number of segments to be created at extension start.
        initial-segments = 1

        # Maximum number of segments that can be created by this byte buffer pool
        # instance. Once this limit will be reached, a next allocation attempt will
        # cause `BufferPoolAllocationException` to be thrown.
        buffer-pool-limit = 1024
      }}

      # A buffer pool used to acquire and release byte buffers from the managed
      # heap. Once byte buffer is no longer needed is can be released, landing 
      # on the pool again, to be reused later. This way we can reduce a GC pressure
      # by reusing the same components instead of recycling them.
      buffer-pool = ""akka.io.tcp.direct-buffer-pool""

      # The number of selectors to stripe the served channels over; each of
      # these will use one select loop on the selector-dispatcher.
	  direct-buffer-pool {{
	    
        # Name of a class used as byte buffer.
        class = ""Akka.IO.Buffers.DirectBufferPool, Akka""

        # The number of bytes per direct buffer in the pool used to read or write
        # network data from the kernel.
        buffer-size = 256 # 256B
        
        # The number of direct byte buffers used per segment. Segment is a default
        # unit of allocation. When buffer pool needs to be resized, instead allocating
        # a direct buffer, we allocate a whole segment of them.
        buffers-per-segment = 250 #250 * 256B = 64 000

        # The number of segments to start with.
        initial-segments = 1
        
        # The maximal number of segments allowed to be allocated within that buffer.
        buffer-pool-limit = 1000
	    }}

	  # A config path to the section defining which byte buffer pool to use.
	  # Buffer pools are used to mitigate GC-pressure made by potentiall allocation
	  # and deallocation of byte buffers used for writing/receiving data from sockets.
	  buffer-pool = ""akka.io.tcp.direct-buffer-pool""

      # The initial number of SocketAsyncEventArgs to be preallocated. This value
	  # will grow infinitely if needed.
      nr-of-socket-async-event-args = 32

      # Maximum number of open channels supported by this TCP module; there is
      # no intrinsic general limit, this setting is meant to enable DoS
      # protection by limiting the number of concurrently connected clients.
      # Also note that this is a ""soft"" limit; in certain cases the implementation
      # will accept a few connections more or a few less than the number configured
      # here. Must be an integer > 0 or ""unlimited"".
      max-channels = 256000

      # When trying to assign a new connection to a selector and the chosen
      # selector is at full capacity, retry selector choosing and assignment
      # this many times before giving up
      selector-association-retries = 10

      # The maximum number of connection that are accepted in one go,
      # higher numbers decrease latency, lower numbers increase fairness on
      # the worker-dispatcher
      batch-accept-limit = 10
	  
      # The duration a connection actor waits for a `Register` message from
      # its commander before aborting the connection.
      register-timeout = 5s

      # The maximum number of bytes delivered by a `Received` message. Before
      # more data is read from the network the connection actor will try to
      # do other work.
      # The purpose of this setting is to impose a smaller limit than the 
      # configured receive buffer size. When using value 'unlimited' it will
      # try to read all from the receive buffer.
      max-received-message-size = unlimited

      # Enable fine grained logging of what goes on inside the implementation.
      # Be aware that this may log more than once per message sent to the actors
      # of the tcp implementation.
      trace-logging = off

      # Fully qualified config path which holds the dispatcher configuration
      # to be used for running the select() calls in the selectors
      selector-dispatcher = ""akka.io.pinned-dispatcher""

      # Fully qualified config path which holds the dispatcher configuration
      # for the read/write worker actors
      worker-dispatcher = ""akka.actor.default-dispatcher""

      # Fully qualified config path which holds the dispatcher configuration
      # for the selector management actors
      management-dispatcher = ""akka.actor.default-dispatcher""

      # Fully qualified config path which holds the dispatcher configuration
      # on which file IO tasks are scheduled
      file-io-dispatcher = ""akka.actor.default-dispatcher""

      # The maximum number of bytes (or ""unlimited"") to transfer in one batch
      # when using `WriteFile` command which uses `FileChannel.transferTo` to
      # pipe files to a TCP socket. On some OS like Linux `FileChannel.transferTo`
      # may block for a long time when network IO is faster than file IO.
      # Decreasing the value may improve fairness while increasing may improve
      # throughput.
      file-io-transferTo-limit = 524288 # 512 KiB

      # The number of times to retry the `finishConnect` call after being notified about
      # OP_CONNECT. Retries are needed if the OP_CONNECT notification doesn't imply that
      # `finishConnect` will succeed, which is the case on Android.
      finish-connect-retries = 5

      # On Windows connection aborts are not reliably detected unless an OP_READ is
      # registered on the selector _after_ the connection has been reset. This
      # workaround enables an OP_CONNECT which forces the abort to be visible on Windows.
      # Enabling this setting on other platforms than Windows will cause various failures
      # and undefined behavior.
      # Possible values of this key are on, off and auto where auto will enable the
      # workaround if Windows is detected automatically.
      windows-connection-abort-workaround-enabled = off
    }}

    udp {{
    
      # Default implementation of `Akka.IO.Buffers.IBufferPool` interface. It
      # allocates memory is so called segments. Each segment is then cut into 
      # buffers of equal size (see: `buffers-per-segment`). Those buffers are 
      # then lend to the requestor. They have to be released later on.
      direct-buffer-pool {{

        # Class implementing `Akka.IO.Buffers.IBufferPool` interface, which
        # will be created with this configuration.
        class = ""Akka.IO.Buffers.DirectBufferPool, Akka""

        # Size of a single byte buffer in bytes.
        buffer-size = 256

        # Number of byte buffers per segment. Every segement is a single continuous
        # byte array in memory. Once buffer pool will run out of byte buffers to 
        # lend it will allocate a next segment of memory. 
        # Each segments size is equal to `buffer-size` * `buffers-per-segment`.
        buffers-per-segment = 500

        # Number of segments to be created at extension start.
        initial-segments = 1

        # Maximum number of segments that can be created by this byte buffer pool
        # instance. Once this limit will be reached, a next allocation attempt will
        # cause `BufferPoolAllocationException` to be thrown.
        buffer-pool-limit = 1024
      }}

      # A buffer pool used to acquire and release byte buffers from the managed
      # heap. Once byte buffer is no longer needed is can be released, landing 
      # on the pool again, to be reused later. This way we can reduce a GC pressure
      # by reusing the same components instead of recycling them.
      buffer-pool = ""akka.io.udp.direct-buffer-pool""

      # The number of selectors to stripe the served channels over; each of
      # these will use one select loop on the selector-dispatcher.
      nr-of-socket-async-event-args = 32
	
	  direct-buffer-pool {{
	    
		# Name of a class used as byte buffer.
	    class = ""Akka.IO.Buffers.DirectBufferPool, Akka""

	    # The number of bytes per direct buffer in the pool used to read or write
	    # network data from the kernel.
	    buffer-size = 256 # 256B
	    
	    # The number of direct byte buffers used per segment. Segment is a default
	    # unit of allocation. When buffer pool needs to be resized, instead allocating
	    # a direct buffer, we allocate a whole segment of them.
	    buffers-per-segment = 250
	    
	    # The maximal number of segments allowed to be allocated within that buffer.
	    buffer-pool-limit = 1000
	  }}

	  # A config path to the section defining which byte buffer pool to use.
	  # Buffer pools are used to mitigate GC-pressure made by potentiall allocation
	  # and deallocation of byte buffers used for writing/receiving data from sockets.
	  buffer-pool = ""akka.io.udp.direct-buffer-pool""

      # The initial number of SocketAsyncEventArgs to be preallocated. This value
	  # will grow infinitely if needed.
      nr-of-socket-async-event-args = 32
	  
      # Maximum number of open channels supported by this UDP module Generally
      # UDP does not require a large number of channels, therefore it is
      # recommended to keep this setting low.
      max-channels = 4096

      # The select loop can be used in two modes:
      # - setting ""infinite"" will select without a timeout, hogging a thread
      # - setting a positive timeout will do a bounded select call,
      #   enabling sharing of a single thread between multiple selectors
      #   (in this case you will have to use a different configuration for the
      #   selector-dispatcher, e.g. using ""type=Dispatcher"" with size 1)
      # - setting it to zero means polling, i.e. calling selectNow()
      select-timeout = infinite

      # When trying to assign a new connection to a selector and the chosen
      # selector is at full capacity, retry selector choosing and assignment
      # this many times before giving up
      selector-association-retries = 10

      # The maximum number of datagrams that are read in one go,
      # higher numbers decrease latency, lower numbers increase fairness on
      # the worker-dispatcher
      receive-throughput = 3

      # The number of bytes per direct buffer in the pool used to read or write
      # network data from the kernel.
      direct-buffer-size = 18432 # 128 KiB

      # The maximal number of direct buffers kept in the direct buffer pool for
      # reuse.
      direct-buffer-pool-limit = 1000

      # The maximum number of bytes delivered by a `Received` message. Before
      # more data is read from the network the connection actor will try to
      # do other work.
      received-message-size-limit = unlimited

      # Enable fine grained logging of what goes on inside the implementation.
      # Be aware that this may log more than once per message sent to the actors
      # of the tcp implementation.
      trace-logging = off

      # Fully qualified config path which holds the dispatcher configuration
      # to be used for running the select() calls in the selectors
      selector-dispatcher = ""akka.io.pinned-dispatcher""

      # Fully qualified config path which holds the dispatcher configuration
      # for the read/write worker actors
      worker-dispatcher = ""akka.actor.default-dispatcher""

      # Fully qualified config path which holds the dispatcher configuration
      # for the selector management actors
      management-dispatcher = ""akka.actor.default-dispatcher""
    }}

    udp-connected {{
	
	  direct-buffer-pool {{
	    
		# Name of a class used as byte buffer.
	    class = ""Akka.IO.Buffers.DirectBufferPool, Akka""

	    # The number of bytes per direct buffer in the pool used to read or write
	    # network data from the kernel.
	    buffer-size = 256 # 256B
	    
	    # The number of direct byte buffers used per segment. Segment is a default
	    # unit of allocation. When buffer pool needs to be resized, instead allocating
	    # a direct buffer, we allocate a whole segment of them.
	    buffers-per-segment = 250
	    
	    # The maximal number of segments allowed to be allocated within that buffer.
	    buffer-pool-limit = 1000
	  }}

	  # A config path to the section defining which byte buffer pool to use.
	  # Buffer pools are used to mitigate GC-pressure made by potentiall allocation
	  # and deallocation of byte buffers used for writing/receiving data from sockets.
	  buffer-pool = ""akka.io.udp-connected.direct-buffer-pool""

      # Default implementation of `Akka.IO.Buffers.IBufferPool` interface. It
      # allocates memory is so called segments. Each segment is then cut into 
      # buffers of equal size (see: `buffers-per-segment`). Those buffers are 
      # then lend to the requestor. They have to be released later on.
      direct-buffer-pool {{

        # Class implementing `Akka.IO.Buffers.IBufferPool` interface, which
        # will be created with this configuration.
        class = ""Akka.IO.Buffers.DirectBufferPool, Akka""

        # Size of a single byte buffer in bytes.
        buffer-size = 256

        # Number of byte buffers per segment. Every segement is a single continuous
        # byte array in memory. Once buffer pool will run out of byte buffers to 
        # lend it will allocate a next segment of memory. 
        # Each segments size is equal to `buffer-size` * `buffers-per-segment`.
        buffers-per-segment = 500

        # Number of segments to be created at extension start.
        initial-segments = 1

        # Maximum number of segments that can be created by this byte buffer pool
        # instance. Once this limit will be reached, a next allocation attempt will
        # cause `BufferPoolAllocationException` to be thrown.
        buffer-pool-limit = 1024
      }}

      # A buffer pool used to acquire and release byte buffers from the managed
      # heap. Once byte buffer is no longer needed is can be released, landing 
      # on the pool again, to be reused later. This way we can reduce a GC pressure
      # by reusing the same components instead of recycling them.
      buffer-pool = ""akka.io.udp-connected.direct-buffer-pool""

      # The number of selectors to stripe the served channels over; each of
      # these will use one select loop on the selector-dispatcher.
      nr-of-socket-async-event-args = 32

      # Maximum number of open channels supported by this UDP module Generally
      # UDP does not require a large number of channels, therefore it is
      # recommended to keep this setting low.
      max-channels = 4096

      # The select loop can be used in two modes:
      # - setting ""infinite"" will select without a timeout, hogging a thread
      # - setting a positive timeout will do a bounded select call,
      #   enabling sharing of a single thread between multiple selectors
      #   (in this case you will have to use a different configuration for the
      #   selector-dispatcher, e.g. using ""type=Dispatcher"" with size 1)
      # - setting it to zero means polling, i.e. calling selectNow()
      select-timeout = infinite

      # When trying to assign a new connection to a selector and the chosen
      # selector is at full capacity, retry selector choosing and assignment
      # this many times before giving up
      selector-association-retries = 10

      # The maximum number of datagrams that are read in one go,
      # higher numbers decrease latency, lower numbers increase fairness on
      # the worker-dispatcher
      receive-throughput = 3

      # The number of bytes per direct buffer in the pool used to read or write
      # network data from the kernel.
      direct-buffer-size = 18432 # 128 KiB

      # The maximal number of direct buffers kept in the direct buffer pool for
      # reuse.
      direct-buffer-pool-limit = 1000

      # The maximum number of bytes delivered by a `Received` message. Before
      # more data is read from the network the connection actor will try to
      # do other work.
      received-message-size-limit = unlimited

      # Enable fine grained logging of what goes on inside the implementation.
      # Be aware that this may log more than once per message sent to the actors
      # of the tcp implementation.
      trace-logging = off

      # Fully qualified config path which holds the dispatcher configuration
      # to be used for running the select() calls in the selectors
      selector-dispatcher = ""akka.io.pinned-dispatcher""

      # Fully qualified config path which holds the dispatcher configuration
      # for the read/write worker actors
      worker-dispatcher = ""akka.actor.default-dispatcher""

      # Fully qualified config path which holds the dispatcher configuration
      # for the selector management actors
      management-dispatcher = ""akka.actor.default-dispatcher""
    }}

    dns {{
      # Fully qualified config path which holds the dispatcher configuration
      # for the manager and resolver router actors.
      # For actual router configuration see akka.actor.deployment./IO-DNS/*
      dispatcher = ""akka.actor.default-dispatcher""

      # Name of the subconfig at path akka.io.dns, see inet-address below
      resolver = ""inet-address""

      inet-address {{
        # Must implement akka.io.DnsProvider
        provider-object = ""Akka.IO.InetAddressDnsProvider""

        # These TTLs are set to default java 6 values
        positive-ttl = 30s
        negative-ttl = 10s

        # How often to sweep out expired cache entries.
        # Note that this interval has nothing to do with TTLs
        cache-cleanup-interval = 120s
      }}
    }}
  }}
  # CoordinatedShutdown is an extension that will perform registered
  # tasks in the order that is defined by the phases. It is started
  # by calling CoordinatedShutdown(system).run(). This can be triggered
  # by different things, for example:
  # - JVM shutdown hook will by default run CoordinatedShutdown
  # - Cluster node will automatically run CoordinatedShutdown when it
  #   sees itself as Exiting
  # - A management console or other application specific command can 
  #   run CoordinatedShutdown
  coordinated-shutdown {{
    # The timeout that will be used for a phase if not specified with 
    # 'timeout' in the phase
    default-phase-timeout = 5 s
    
    # Terminate the ActorSystem in the last phase actor-system-terminate.
    terminate-actor-system = on
    
    # Exit the CLR(Environment.Exit(0)) in the last phase actor-system-terminate
    # if this is set to 'on'. It is done after termination of the 
    # ActorSystem if terminate-actor-system=on, otherwise it is done 
    # immediately when the last phase is reached.  
    exit-clr = off
    
    # Run the coordinated shutdown when the CLR process exits, e.g.
    # via kill SIGTERM signal (SIGINT ctrl-c doesn't work).
    run-by-clr-shutdown-hook = on
  
    #//#coordinated-shutdown-phases
    # CoordinatedShutdown will run the tasks that are added to these
    # phases. The phases can be ordered as a DAG by defining the 
    # dependencies between the phases.  
    # Each phase is defined as a named config section with the
    # following optional properties:
    # - timeout=15s: Override the default-phase-timeout for this phase.
    # - recover=off: If the phase fails the shutdown is aborted
    #                and depending phases will not be executed.
    # depends-on=[]: Run the phase after the given phases
    phases {{

      # The first pre-defined phase that applications can add tasks to.
      # Note that more phases can be be added in the application's
      # configuration by overriding this phase with an additional 
      # depends-on.
      before-service-unbind {{
      }}
    
      # Stop accepting new incoming requests in for example HTTP.
      service-unbind {{
        depends-on = [before-service-unbind]
      }}
      
      # Wait for requests that are in progress to be completed.
      service-requests-done {{
        depends-on = [service-unbind]
      }}
      
      # Final shutdown of service endpoints.
      service-stop {{
        depends-on = [service-requests-done]
      }}
      
      # Phase for custom application tasks that are to be run
      # after service shutdown and before cluster shutdown.
      before-cluster-shutdown {{
        depends-on = [service-stop]
      }}
      
      # Graceful shutdown of the Cluster Sharding regions.
      cluster-sharding-shutdown-region {{
        timeout = 10 s
        depends-on = [before-cluster-shutdown]
      }}
      
      # Emit the leave command for the node that is shutting down.
      cluster-leave {{
        depends-on = [cluster-sharding-shutdown-region]
      }}
      
      # Shutdown cluster singletons
      cluster-exiting {{
        timeout = 10 s
        depends-on = [cluster-leave]
      }}
      
      # Wait until exiting has been completed
      cluster-exiting-done {{
        depends-on = [cluster-exiting]
      }}
      
      # Shutdown the cluster extension
      cluster-shutdown {{
        depends-on = [cluster-exiting-done]
      }}
      
      # Phase for custom application tasks that are to be run
      # after cluster shutdown and before ActorSystem termination.
      before-actor-system-terminate {{
        depends-on = [cluster-shutdown]
      }}
      
      # Last phase. See terminate-actor-system and exit-jvm above.
      # Don't add phases that depends on this phase because the 
      # dispatcher and scheduler of the ActorSystem have been shutdown. 
      actor-system-terminate {{
        timeout = 10 s
        depends-on = [before-actor-system-terminate]
      }}
    }}
    #//#coordinated-shutdown-phases
  }}
}}";

            return config;
        }
    }
}
