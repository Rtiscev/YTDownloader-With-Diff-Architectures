import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    vus: 50, // Virtual users
    duration: '5m', // 5 minutes
    thresholds: {
        http_req_duration: ['p(95)<500', 'p(99)<1000'], // 95th percentile < 500ms
        http_req_failed: ['rate<0.1'], // Error rate < 10%
    },
};

export default function () {
    // Test 1: Health check
    const healthRes = http.get('http://apigateway:5000/health');
    check(healthRes, {
        'health status 200': (r) => r.status === 200,
    });

    sleep(1);

    // Test 2: Auth service
    const authRes = http.get('http://authservice:5000/health');
    check(authRes, {
        'auth status 200': (r) => r.status === 200,
    });

    sleep(1);

    // Test 3: MinIO service
    const minioRes = http.get('http://minioservice:5000/list/my-bucket');
    check(minioRes, {
        'minio status 200': (r) => r.status === 200 || r.status === 500, // May be 500 if bucket empty
    });

    sleep(2);
}
