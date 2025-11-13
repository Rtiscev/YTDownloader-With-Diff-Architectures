import { NextRequest, NextResponse } from 'next/server'

const BACKEND_AUTH_ME_URL = 'http://apigateway:5000/api/auth/me'

export async function GET(req: NextRequest) {
    const authHeader = req.headers.get('authorization')
    if (!authHeader) {
        return NextResponse.json({ message: 'Unauthorized' }, { status: 401 })
    }

    try {
        const backendRes = await fetch(BACKEND_AUTH_ME_URL, {
            method: 'GET',
            headers: { Authorization: authHeader },
        })

        const data = await backendRes.json()

        if (!backendRes.ok) {
            return NextResponse.json(data, { status: backendRes.status })
        }

        return NextResponse.json(data)
    } catch (error) {
        console.error('[Auth ME] Error:', error)
        return NextResponse.json({ message: 'Failed to reach auth service' }, { status: 500 })
    }
}
