import type { NextRequest } from 'next/server'

export async function POST(request: NextRequest) {
    try {
        const body = await request.json()

        // Health check API Gateway
        try {
            const healthResponse = await fetch('http://apigateway:5000/health')
            if (!healthResponse.ok) {
                return Response.json(
                    { success: false, message: 'API Gateway is not healthy' },
                    { status: 503 }
                )
            }
        } catch {
            return Response.json(
                { success: false, message: 'Cannot reach API Gateway' },
                { status: 503 }
            )
        }

        const response = await fetch('http://apigateway:5000/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        })

        const data = await response.json()
        return Response.json(data, { status: response.status })
    } catch (error) {
        console.error('[Register] Error:', error)
        return Response.json({ success: false, message: 'Server error' }, { status: 500 })
    }
}
