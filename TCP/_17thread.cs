using System.Diagnostics;

namespace thread_console_msdn_01
{
    internal class thread_test
    {
        // 예제 데이터 경합을 위한 Lock 에사용
        static Object obj = new Object();

        public static void Main()
        {
            // Thread 클래스 사이트에 나온 예제 참고.
            // 예제 1.
            // Thread 를 생성하는 가장 기본적인 방법
            // Thread 객체를 생성하고 실행.
            Console.WriteLine("Main Thread Start {0}: {1}, Priority {2}",
                              Thread.CurrentThread.ManagedThreadId,
                              Thread.CurrentThread.ThreadState,
                              Thread.CurrentThread.Priority);

            // public delegate void ThreadStart();
            //Thread th1 = new Thread(ExecuteInForeground);
            // 오버로딩되는 함수를 정확히 인식하지 못하는 문제
            // 타입을 정확히 지정하면 방법으로 처리
            //Thread th2 = new Thread(ExecuteInForeground);

            Thread th1 = new Thread(new ThreadStart(ExecuteInForeground));
            // public delegate void ParameterizedThreadStart(object obj);
            Thread th2 = new Thread(new ParameterizedThreadStart(ExecuteInForeground));
            // 스레드는 Foreground 타입과 Background 타입이 있음
            // Foreground , Main Thread가 종료되도 남은 작업을 마치고 Work Thread가 종료되는 타입
            // Background , Main Thread가 종료되면 Work Thread 도 같이 종료함
            // Main Thread가 종료되면 잔여시간(<10000)이 있어도 종료됨.
            //th1.IsBackground = true;
            // 스레드 th1 시작 
            th1.Start();
            // 스레드 th2 시작 전달 인자값으로 1500 전달 하면 시작
            // 전달되는 인자 값은 object 타입 --> 어떤 타입도 전달될수 있다
            th2.Start(1500);
            Thread.Sleep(1000);

            // 예제 2
            // 오전 반은 UI 스레드만 했음 다시 설명하겠음.
            // 오후 반은 UI 스레드를 못해서 그부분을 다시 설명하겠음.
            // thread 10K 만들기
            // 10K 만들면 느리므로 1K로 진행해도 됨
            // 대략 학교 컴퓨터에서 1초에 100threads 가 생성, 동작, 종료함.
            //Console.WriteLine("Work thread 10K make and start...");
            //for (int idx = 0; idx < 10000; idx++)
            //{
            //    var thread = new Thread(new ParameterizedThreadStart(loopTEST));
            //    thread.Start((double)idx);
            //    // 동작 중인 스레드를 멈추려고 할때.
            //    // 사용하려면 예외처리 해야함.
            //    if (idx % 10 == 0) thread.Interrupt();
            //}
            // 10K 만들고 한번에 실행하기
            // 10K 만들면 느리므로 1K로 진행해도 됨
            Console.WriteLine("Work thread 10K make all of thread, and all of thread start...");
            Thread[] thArray = new Thread[1000];
            for (int idx = 0; idx < 1000; idx++)
            {
                thArray[idx] = new Thread(new ParameterizedThreadStart(loopTEST));
            }
            double radius = 0;
            foreach (var thread in thArray)
            {
                thread.Start(radius++);
                // 사용하려면 예외처리 해야함.
                if ((int)radius % 10 == 0) thread.Interrupt();
            }

            // Thread 작업 10K threadPool 사용해서 처리하기
            // ThreadPool 큐에 작업을 넣어주면 자동으로 실행
            Console.WriteLine("Work thread 10K in ThreadPool.................................");
            for (double i = 0; i < 1000; i++)
            {
                ThreadPool.QueueUserWorkItem(loopTEST, i);
            }

            // 예제 3.
            // Thread 정보 확인하기
            // 중요 정보 
            // 1. 스레드 상태
            // 2. 스레드 우선순위
            // 3. 스레드, 스레드풀 타입 확인
            // 4. 스레드 동작방식,  백그라운드, 포그라운드 확인
            Console.WriteLine("Work thread information.................................");
            ThreadPool.QueueUserWorkItem(ShowThreadInformation);
            var th3 = new Thread(ShowThreadInformation);
            th3.Start();
            var th4 = new Thread(ShowThreadInformation);
            th4.IsBackground = true;
            th4.Start();
            Thread.Sleep(500);
            ShowThreadInformation(null);

            // 예제 4.
            // 스레드 흐름 제어하기
            // 스레드가 Work Thread Join을 호출하면 해당 스레드가 종료되어 
            // 코드 흐름이 넘어 오기 전까지 다음 구문을 실행하지 않는다
            // 이부분은 다시 할 예정으로 지금  넘어가도 됨

            // th1이 10초 정도 동작하므로 Main Thread가 먼저 끝나지 못하고
            // 대기하였다가 th1.이 종료하면 Join() 코드 다음 라인이 실행함.
            // 다음 줄의 주석을 제거한 것과 아닌것을 비교해보면 됨.
            Console.WriteLine("if you call th1.Join(), MainThread was waiting that thread (th1) finish working.....................................");
            th1.Join();
            
            Console.WriteLine("Finish working thread th1..................................................................");
            Console.WriteLine("Main thread ({0}) exiting...",
                              Thread.CurrentThread.ManagedThreadId);
            // ThreadPool 사용시 ThreadPool 의 Worker Thread들은 Background 스레드로 
            // 동작한다 따라서 메인 스레드가 끝나면  남은 작업도 기다리지 않고 같이 끝나기때문에
            // 다음 콘솔 ReadLine을 사용하여 MainThread가 유지되도록 할때 임시로 사용함.
            Console.ReadLine();
        }
        private static void ShowThreadInformation(Object state)
        {
            lock (obj)
            {
                var th = Thread.CurrentThread;
                Console.WriteLine("Managed thread #{0}: ", th.ManagedThreadId);
                Console.WriteLine("   Background thread: {0}", th.IsBackground);
                Console.WriteLine("   Thread pool thread: {0}", th.IsThreadPoolThread);
                Console.WriteLine("   Priority: {0}", th.Priority);
                Console.WriteLine("   Culture: {0}", th.CurrentCulture.Name);
                Console.WriteLine("   UI culture: {0}", th.CurrentUICulture.Name);
                Console.WriteLine();
            }
        }
        private static void loopTEST(object obj)
        {
            try
            {
                if (obj == null) return;

                double r = (double)obj;

                if (r == 0.0)
                {
                    Console.WriteLine("Zero Error");
                    return;
                }

                double result = r * r * 3.14;
                Console.WriteLine($"ID:{Thread.CurrentThread.ManagedThreadId}==> r={r}, circle-area={result,5:N2}");
            }
            catch (ThreadInterruptedException ex)
            {
                Console.WriteLine($"ID:{Thread.CurrentThread.ManagedThreadId}");
            }
        }

        private static void ExecuteInForeground()
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine("Work Thread Start {0}: {1}, Priority {2}",
                              Thread.CurrentThread.ManagedThreadId,
                              Thread.CurrentThread.ThreadState,
                              Thread.CurrentThread.Priority);
            do
            {
                Console.WriteLine("Work Thread {0}: Elapsed {1:N2} seconds",
                                  Thread.CurrentThread.ManagedThreadId,
                                  sw.ElapsedMilliseconds / 1000.0);
                Thread.Sleep(500);
            } while (sw.ElapsedMilliseconds <= 10000);
            sw.Stop();
            Console.WriteLine("Work thread ({0}) exiting...",
                              Thread.CurrentThread.ManagedThreadId);
        }
        private static void ExecuteInForeground(Object obj)
        {
            int interval;
            try
            {
                interval = (int)obj;
            }
            catch (InvalidCastException)
            {
                interval = 5000;
            }
            var sw = Stopwatch.StartNew();
            Console.WriteLine("Work Thread {0}: {1}, Priority {2}",
                              Thread.CurrentThread.ManagedThreadId,
                              Thread.CurrentThread.ThreadState,
                              Thread.CurrentThread.Priority);
            do
            {
                Console.WriteLine("Thread {0}: Elapsed {1:N2} seconds",
                                  Thread.CurrentThread.ManagedThreadId,
                                  sw.ElapsedMilliseconds / 1000.0);
                Thread.Sleep(500);
            } while (sw.ElapsedMilliseconds <= interval);
            sw.Stop();
            Console.WriteLine("Work thread ({0}) exiting...",
                              Thread.CurrentThread.ManagedThreadId);
        }
    }
}
