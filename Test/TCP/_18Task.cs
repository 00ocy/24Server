
namespace console_Task_test_01
{
    internal class Task_test
    {
        static object obj = new object();
        static CancellationTokenSource cts;
        static void Main(string[] args)
        {
            // 예제 1. Task 클래스
            // MainThread 정보 확인
            ShowThreadInformation(null);
            // Task 준비
            Console.WriteLine("[info] --> Ready....TASK1....");
            // Task와 Thread 사용방법 차이를 위한 비교
            var thread = new Thread( new ThreadStart(Run) );
            // Task 생성
            // 1. 함수 지정
            var task1 = new Task(Run);
            // 2. 명시적으로 Action 타입 함수 지정
            var task2 = new Task(new Action(Run));
            // 3. 람다 식을 사용한 함수 지정
            var task3 = new Task(() =>
            {
                Console.WriteLine("Task Calss TESING with LAMDA_01");
                ShowThreadInformation(null);
            }); 
            // 4. 람다 식 사용 및 지연시간 추가
            var task4 = new Task(() =>
            {
                Thread.Sleep(1500);
                Console.WriteLine("Task Calss TESING with LAMDA_02");
                ShowThreadInformation(null);
            });
            // Thread, Task 시작  
            thread.Start();
            task1.Start();
            task2.Start();
            task3.Start();
            task4.Start();

            // MainThread 대기 방법
            // 임시로 콘솔에 문자 입력 전까지 대기
            //Console.ReadLine();
            // Thread 종료까지 기다림
            thread.Join();  
            // Task 종료까지 기다림
            task4.Wait();

            // 예제 2. Task<TResult> 클래스
            // Task 클래스 사용중 중지하고자 할때 Cancel Token 준비
            cts = new CancellationTokenSource();
            var token = cts.Token;

            // 1. 반환형식 int를 갖는 람다식 등록
            var taskTR = new Task<int>(() => { Thread.Sleep(1000); return 2000; });
            // 2. 반환형식 int를 갖는 RunTR 함수 등록 과 취소를 위한 token 등록
            var taskTR2 = new Task<int>(new Func<int>(RunTR),token);
            // 3. Task 등록과 함께 바로 시작하기 위한 방법
            var taskTR3 = Task.Factory.StartNew<int>(RunTR);

            // Task<TResult> 시작
            taskTR.Start();
            taskTR2.Start();
            // taskTR2 작업 종료 알림 
            cts.Cancel();

            Console.WriteLine("Waiting task in MainThread.............");
            // Task 클래스 내부에서 ThreadPool을 사용하므로 결과가 없는 작업을 진행하면
            // Main Thread와 함께 종료
            // 1. Main Thread 강제 대기
            //Thread.Sleep(2000);
            // 2. Task wait
            // taskTR3.Wait();

            //taskTR.Wait(); 과 동일한 효과
            // 결과를 획득하기 위해서 자동으로 wait하고 결과를 반환
            // task의 Result 속성을 통해서 결과를 획득함.
            int result1 = taskTR.Result;
            int result2 = taskTR2.Result;   
            int result3 = taskTR3.Result;

            Console.WriteLine($"taskTR1 is {result1}");
            Console.WriteLine($"taskTR2 is {result2}");
            Console.WriteLine($"taskTR3 is {result3}");

        }

        // 단일문장 함수에서 중괄호 없이 함수 생성법
        // 람다식은 이런 형태의 무명함수로 생성 ()=> { ; }
        // 함수는 이름이 존재하여 재활용할수 있지만 무명함수는 한번만 사용
        
        static void LamdaTEST() => Console.WriteLine("LamdaTESTING");

        // Task<TResult> 를 위한 함수
        // TResult를 int로 지정한 Task<T>에서 사용
        static int RunTR()
        {
            // 전달된 token을 통해서 중지명령을 받았는지 확인하기 위한 부분
            while (!cts.IsCancellationRequested)
            {
                Thread.Sleep(2000);
                return 2000;
            }

            return 10;
        }

        // Task 를 위한 함수
        private static void Run()
        {
            Console.WriteLine("Task class TESTING without Argument");
            ShowThreadInformation(null);
        }
        // Task 내부의 스레드 정보를 확인하는 함수
        private static void ShowThreadInformation(Object state)
        {
            lock (obj)
            {
                var th = Thread.CurrentThread;
                Console.WriteLine("Managed thread #{0}: ", th.ManagedThreadId);
                Console.WriteLine("   Background thread: {0}", th.IsBackground);
                Console.WriteLine("   Thread pool thread: {0}", th.IsThreadPoolThread);
                Console.WriteLine();
            }
        }
    }
}
