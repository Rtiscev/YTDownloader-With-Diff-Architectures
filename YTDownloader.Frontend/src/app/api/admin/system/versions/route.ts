import { NextRequest, NextResponse } from 'next/server'

function verifyAuth(request: NextRequest): { valid: boolean; error?: string } {
    const authHeader = request.headers.get('authorization')

    if (!authHeader) {
        return { valid: false, error: 'Unauthorized' }
    }

    if (!authHeader.startsWith('Bearer ')) {
        return { valid: false, error: 'Invalid authorization format' }
    }

    const token = authHeader.split(' ')[1]
    if (!token) {
        return { valid: false, error: 'No token provided' }
    }

    return { valid: true }
}

export async function GET(request: NextRequest) {
    const authCheck = verifyAuth(request)
    if (!authCheck.valid) {
        return NextResponse.json({ error: authCheck.error }, { status: 401 })
    }

    try {
        const authHeader = request.headers.get('authorization')

        const response = await fetch('http://localhost:5000/api/system/versions', {
            method: 'GET',
            headers: {
                'Authorization': authHeader || '',
                'Content-Type': 'application/json',
            },
        })

        if (response.status === 401) {
            return NextResponse.json({ error: 'Unauthorized' }, { status: 401 })
        }

        if (!response.ok) {
            throw new Error(`Backend error: ${response.status}`)
        }

        const data = await response.json()
        return NextResponse.json(data)
    } catch (error) {
        console.error('[Versions API] Error:', error)
        return NextResponse.json({ error: 'Failed to fetch system versions' }, { status: 500 })
    }
}
