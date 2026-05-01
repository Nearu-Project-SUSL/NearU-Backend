import http from 'k6/http';
import { check, sleep } from 'k6';

// 1. Configuration (Options)
export const options = {
    stages: [
        { duration: '30s', target: 10 }, // Ramp up to 10 users
        { duration: '1m', target: 10 },  // Stay at 10 users (Steady state)
        { duration: '30s', target: 0 },  // Ramp down to 0
    ],
    thresholds: {
        http_req_failed: ['rate<0.01'],   // Errors must be < 1%
        http_req_duration: ['p(95)<300'], // 95% of requests must be < 300ms
    },
};

// 2. VU Execution (The "User Flow")
export default function () {
    const url = 'https://api.nearusab.me/healthz';
    const res = http.get(url);

    // Checks help verify functional correctness under load
    check(res, {
        'is status 200': (r) => r.status === 200,
        'body has "Healthy"': (r) => r.body.includes('Healthy'),
    });

    sleep(1); // Realistic user "think time"
}