using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocol
{
    public class DummyData
    {
        // 더미 파일 생성 메서드
        public static void CreateDummyFile(string filePath, long sizeInBytes)
        {
            byte[] data = new byte[1024]; // 1KB 크기의 데이터 블록 생성

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                long remainingBytes = sizeInBytes;

                // 남은 크기만큼 파일에 데이터 쓰기
                while (remainingBytes > 0)
                {
                    int bytesToWrite = (int)Math.Min(data.Length, remainingBytes); // 한번에 쓸 수 있는 데이터 크기 계산
                    fs.Write(data, 0, bytesToWrite); // 파일에 데이터 쓰기
                    remainingBytes -= bytesToWrite;  // 남은 크기 줄이기
                }
            }

            Console.WriteLine($"{filePath} ({sizeInBytes / 1024 / 1024}MB) 파일이 생성되었습니다.");
        }
    }
}
