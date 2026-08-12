import http from 'k6/http';
import { check, sleep } from 'k6';

// Environment base URL (Defaults to hosted endpoint, or passed via __ENV.BASE_URL for local testing)
const BASE_URL = __ENV.BASE_URL || 'https://api.nearusab.me';

// 1. Configuration Options
export const options = {
    stages: [
        { duration: '30s', target: 10 }, // Ramp up to 10 VUs
        { duration: '1m', target: 10 }, // Hold steady at 10 VUs
        { duration: '30s', target: 0 }, // Ramp down to 0
    ],
    thresholds: {
        http_req_failed: ['rate<0.01'],   // Error rate must be less than 1%
        http_req_duration: ['p(95)<1000'], // 95% of requests must respond in < 1000ms
    },
};

// 2. VU Execution Flow
export default function () {
    // Test GET /api/Job endpoint
    const url = `${BASE_URL}/api/Job?page=1&pageSize=10`;
    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.get(url, params);

    // Verify status and JSON response payload
    check(res, {
        'status is 200': (r) => r.status === 200,
        'response is valid JSON': (r) => {
            try {
                const body = JSON.parse(r.body);
                return body.success === true;
            } catch (e) {
                return false;
            }
        },
    });

    sleep(1); // Realistic user think time
}
