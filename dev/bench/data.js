window.BENCHMARK_DATA = {
  "lastUpdate": 1778182585824,
  "repoUrl": "https://github.com/Nearu-Project-SUSL/NearU-Backend",
  "entries": {
    "Benchmark": [
      {
        "commit": {
          "author": {
            "name": "Thimira Niranjaya",
            "email": "Thimira Niranjaya"
          },
          "committer": {
            "name": "Thimira Niranjaya",
            "email": "Thimira Niranjaya"
          },
          "id": "caa63c33e369712d3450cfc205695bbc95b4f370",
          "message": "fix: correct BenchmarkDotNet artifact path to repo root (CWD of dotnet run)",
          "timestamp": "2026-05-07T19:27:48Z",
          "url": "https://github.com/Nearu-Project-SUSL/NearU-Backend/commit/caa63c33e369712d3450cfc205695bbc95b4f370"
        },
        "date": 1778182585242,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "NearUPerformanceBenchmarks.GenerateJwtToken",
            "value": 7942.584864298503,
            "unit": "ns",
            "range": "± 67.54820879109576"
          },
          {
            "name": "NearUPerformanceBenchmarks.VerifyPassword",
            "value": 145462766.16666666,
            "unit": "ns",
            "range": "± 37632.40406578928"
          },
          {
            "name": "NearUPerformanceBenchmarks.SerialiseAccommodationList",
            "value": 14291.31596883138,
            "unit": "ns",
            "range": "± 124.54197250024784"
          },
          {
            "name": "NearUPerformanceBenchmarks.FilterActiveAccommodations",
            "value": 1252.0546449025471,
            "unit": "ns",
            "range": "± 1.7544137627461487"
          },
          {
            "name": "NearUPerformanceBenchmarks.DeserialiseAccommodation",
            "value": 393.02863613764447,
            "unit": "ns",
            "range": "± 1.593353285902516"
          }
        ]
      }
    ]
  }
}