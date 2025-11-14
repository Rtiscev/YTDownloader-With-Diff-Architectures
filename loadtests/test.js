import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
    stages: [
        { duration: '2m', target: 50 },    // Ramp up to 50 users
        { duration: '5m', target: 100 },   // Increase to 100 users
        { duration: '5m', target: 200 },   // Stress: 200 users
        { duration: '5m', target: 500 },   // Heavy stress: 500 users
        { duration: '2m', target: 1000 },  // SPIKE: 1000 users (find breaking point)
        { duration: '3m', target: 1000 },  // Hold at peak
        { duration: '2m', target: 0 },     // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(99)<1000'],  // 99th percentile under 1 second (relaxed)
        http_req_failed: ['rate<0.05'],     // Allow 5% error rate (more permissive)
    },
};

export default function () {
    let loginRes = http.post('http://apigateway:5000/api/auth/login',
        JSON.stringify({
            email: 'admin@yt.com',
            password: 'Admin47)'
        }),
        { headers: { 'Content-Type': 'application/json' } }
    );

    check(loginRes, {
        'login successful': (r) => r.status === 200,
        'has token': (r) => r.json('token') !== '',
    });

    let token = loginRes.json('token');

    let profileRes = http.get('http://apigateway:5000/api/auth/me', {
        headers: { 'Authorization': `Bearer ${token}` },
    });

    check(profileRes, {
        'profile retrieved': (r) => r.status === 200,
    });

    sleep(0.5); // Reduced from 1s to increase request rate
}
