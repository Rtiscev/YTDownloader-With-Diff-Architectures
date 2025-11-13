import { NextRequest, NextResponse } from 'next/server'

export interface User {
    id: string
    username: string
    email: string
    createdAt: string
    lastLogin?: string
}

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

function handleAuthError(status: number): NextResponse | null {
    if (status === 401) {
        return NextResponse.json({ error: 'Unauthorized' }, { status: 401 })
    }
    if (status === 403) {
        return NextResponse.json({ error: 'Forbidden' }, { status: 403 })
    }
    return null
}

export async function GET(request: NextRequest) {
    const authCheck = verifyAuth(request)
    if (!authCheck.valid) {
        return NextResponse.json({ error: authCheck.error }, { status: 401 })
    }

    try {
        const authHeader = request.headers.get('authorization')

        const response = await fetch('http://apigateway:5000/api/auth/users', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authHeader || '',
            },
            cache: 'no-store',
        })

        const authError = handleAuthError(response.status)
        if (authError) return authError

        if (!response.ok) {
            throw new Error(`Auth service error: ${response.status}`)
        }

        const users: User[] = await response.json()
        return NextResponse.json(users)
    } catch (error) {
        console.error('[Users API] GET error:', error)
        return NextResponse.json({ error: 'Failed to fetch users' }, { status: 500 })
    }
}

export async function DELETE(request: NextRequest) {
    const authCheck = verifyAuth(request)
    if (!authCheck.valid) {
        return NextResponse.json({ error: authCheck.error }, { status: 401 })
    }

    try {
        const { searchParams } = new URL(request.url)
        const userId = searchParams.get('userId')
        const authHeader = request.headers.get('authorization')

        if (!userId) {
            return NextResponse.json({ error: 'userId is required' }, { status: 400 })
        }

        const response = await fetch(`http://apigateway:5000/api/auth/users/${userId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authHeader || '',
            },
        })

        const authError = handleAuthError(response.status)
        if (authError) return authError

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}))
            throw new Error(errorData.error || `Delete failed: ${response.status}`)
        }

        return NextResponse.json({ success: true, message: 'User deleted successfully' })
    } catch (error) {
        console.error('[Users API] DELETE error:', error)
        return NextResponse.json(
            { error: error instanceof Error ? error.message : 'Failed to delete user' },
            { status: 500 }
        )
    }
}
