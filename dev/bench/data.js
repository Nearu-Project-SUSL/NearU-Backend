window.BENCHMARK_DATA = {
  "lastUpdate": 1778320132030,
  "repoUrl": "https://github.com/Nearu-Project-SUSL/NearU-Backend",
  "entries": {
    "Benchmark": [
      {
        "commit": {
          "author": {
            "email": "tnirajaya2001@gmail.com",
            "name": "Niranjaya Keerthiwansha",
            "username": "thimira20011"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "f652810e8750e2c7b7d4d02682502ef475e29425",
          "message": "Merge pull request #111 from Nearu-Project-SUSL/NU-38-Rides-Mobility\n\nAdd performance benchmarking and implement ride domain features",
          "timestamp": "2026-05-09T15:17:38+05:30",
          "tree_id": "c1fdced6e75978be8aecaef1d1ecb784311f7b61",
          "url": "https://github.com/Nearu-Project-SUSL/NearU-Backend/commit/f652810e8750e2c7b7d4d02682502ef475e29425"
        },
        "date": 1778320131780,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "NearUPerformanceBenchmarks.GenerateJwtToken",
            "value": 7281.070627848308,
            "unit": "ns",
            "range": "± 114.68278943944475"
          },
          {
            "name": "NearUPerformanceBenchmarks.VerifyPassword",
            "value": 146572503.83333334,
            "unit": "ns",
            "range": "± 226083.21994073517"
          },
          {
            "name": "NearUPerformanceBenchmarks.SerialiseAccommodationList",
            "value": 14733.055836995443,
            "unit": "ns",
            "range": "± 26.649135506303253"
          },
          {
            "name": "NearUPerformanceBenchmarks.FilterActiveAccommodations",
            "value": 1305.2738806406658,
            "unit": "ns",
            "range": "± 13.255182136485393"
          },
          {
            "name": "NearUPerformanceBenchmarks.DeserialiseAccommodation",
            "value": 458.2981266975403,
            "unit": "ns",
            "range": "± 1.2787052248302588"
          }
        ]
      }
    ]
  }
}