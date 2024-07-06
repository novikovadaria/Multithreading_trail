
#region Interlocked

//_i++; так нельзя
//Чтобы выполнять атомарные операции, 
//нужно использовать специальные классы:

//Interlocked.Increment(ref _i);
//Interlocked.Decrement(ref _i);

//var x = Interlocked.Exchange(ref _i, 1); - в _i положит 1, то что было вернёт
//var x = Interlocked.CompareExchange(ref _i, 9, 10); - в _i положит 9,
//если там 10, и вернёт чтобы не было в  _i

//_i будет увеличена столько раз, сколько потоков вызвано
#endregion

#region _lock

//private readonly object _lock = new object();

//критическая секция
//здесь может выполняться только 1 поток

//_lock - идентификатор секции

//lock (_lock)
//{

//}

#endregion

#region MyThreadPool

class MyThreadPool : IDisposable
{
    private readonly Thread[] _threads;
    private readonly Queue<Action> _actions;

    private readonly object _lock = new object();

    public MyThreadPool(int count = 4) 
    {
        _actions = new Queue<Action>();

        _threads = new Thread[count];
        for (int i = 0; i < _threads.Length; i++) 
        {
            _threads[i] = new Thread(ThreadProc)
            {
                IsBackground = true,
                Name = $"MyThreadPool Thread {i}"
            };

            _threads[i].Start();
        }
    }

    //потокобезопасный метод
    public void Queue(Action action)
    {
        Monitor.Enter(_lock);
        try 
        { 
            _actions.Enqueue(action);
            
            //будем поток если есть хоть одно действие
            //Monitor.PulseAll(_lock); - будет все потоки.
            //Но нам не надо, тк у нас только 1 действия, и остальные потоки все равно заснут

            if(_actions.Count == 1) 
            { 
                Monitor.Pulse(_lock);
            }
        }
        finally
        {
            Monitor.Exit(_lock);    
        }
    }

    #region Threadsave ThreadProc but CPU suffers

    //обеспечили синхронизацию,
    //но из за бесполезной прокрутки грузит cpu
    //void ThreadProc()
    //{
    //    while (true)
    //    {
    //        Action action;
    //        Monitor.Enter(_lock) ;
    //        try
    //        {
    //            if (_actions.Count > 0)
    //            {
    //                action = _actions.Dequeue();
    //            }
    //            else
    //            {
    //                //Если очередь пуста, потоки просто продолжают выполнять цикл,
    //                //что вызывает значительное потребление процессорного времени.
    //                continue;
    //            }
    //        }
    //        finally
    //        {
    //            Monitor.Exit(_lock);
    //        }
    //        action();
    //    }
    //}

    #endregion

    #region Threadsave ThreadProc and CPU is ok

    void ThreadProc()
    {
        while (true)
        {
            Action action;
            Monitor.Enter(_lock);
            try
            {
                if (IsDisposed)
                {
                    return;
                }

                if (_actions.Count > 0)
                {
                    action = _actions.Dequeue();
                }
                else
                {
                    Monitor.Wait(_lock); //только внутри критической секции
                    //Thread.Sleep(1000); // невозможно прервать
                    continue;
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }
            action();
        }
    }

    #endregion

    #region Dispose

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        bool isDisposing = false;
        if (!IsDisposed)
        {
            Monitor.Enter(_lock);
            try 
            {
                if (!IsDisposed)
                { 
                    IsDisposed = true; 
                    Monitor.PulseAll(_lock);
                    isDisposing = true;
                }
            }
            finally
            { 
                Monitor.Exit(_lock); 
            }

            if (isDisposing)
            {
                for (int i = 0; i < _threads.Length; i++)
                {
                    // join() блокирует вызывающий поток, пока не закончится тот поток, что его вызвал
                    _threads[i].Join();
                }
            }
        }
    }

    #endregion
}

#endregion

#region Sync Primitives

//их надо деспойзить!!!

//Критическая секция. Второй зайти не может пока первый не освободит.
//var mutex = new Mutex();

////Тоже самое но зайти могут несколько
//var semaphore = new Semaphore(initialCount: 5, maximumCount: 5);

//--------------------------------------------------
//var mevnt = new ManualResetEvent(initialState : false);
//var aevnt = new AutoResetEvent(initialState: false);

//Set(): Устанавливает состояние события в сигнальное (true),
//что приводит к продолжению всех ожидающих потоков.

//Reset(): Сбрасывает состояние события в невыполненное (false).

//WaitOne(): Блокирует текущий поток до получения сигнала от события.

//WaitOne(timeout): Блокирует текущий поток до получения сигнала от события
//или истечения указанного времени ожидания.
//--------------------------------------------------

//var x = new ReaderWriterLock();

#endregion

class Program
{
    #region Main One Thread

    //static void Main(string[] args)
    //{
        //var thread = new Thread(ThreadProc)
        //{
        //    IsBackground = true, // флаг фонового потока
        //                         //Программа может завершиться, даже если активны фоновые потоки.
        //    Priority = ThreadPriority.Normal,
        //    Name = "имя для дебага"
        //};

        //thread.Start();

        //ThreadPool - система создаёт n-коллво потоков, колво зависит от числа ядер процессора

        //ThreadPool.QueueUserWorkItem(state => Console.WriteLine("From Thread Pool"));

        //поток выполнит указанную операцию и вернётся в пул.
        //.net будет конкурировать со мной при использовании ThreadPool


        //Console.ReadKey();
    //}

    #endregion

    #region Main ThreadPool

    static void Main(string[] args)
    {
        using (var pool = new MyThreadPool())
        {
            pool.Queue(() => Console.WriteLine("From My Thread Pool"));
        }
    }

    #endregion

}