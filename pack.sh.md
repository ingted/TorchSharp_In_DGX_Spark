   1 chmod +x /workspace/TorchSharp_In_DGX_Spark_fp4/pack.sh
   2 /workspace/TorchSharp_In_DGX_Spark_fp4/pack.sh


  提示：
   1. 執行前請確保您已經完成了 C++ 原生庫和 C# 受控 DLL 的編譯（通常是執行 build_TorchSharp.Native.sh 和 build_TorchSharp.net.sh），因為打包過程會去 bin 目錄搜尋這些檔案。
   2. 如果 pack.proj 內部定義了特定的 PackageId 或 Version，生成的檔名會依據其定義。

