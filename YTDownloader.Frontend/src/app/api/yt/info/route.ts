import { NextRequest, NextResponse } from 'next/server'

const BACKEND_INFO_URL = 'http://localhost:5000/api/download/info?url='

export async function GET(req: NextRequest) {
    try {
        const videoUrl = req.nextUrl.searchParams.get('url')
        if (!videoUrl) {
            return NextResponse.json({ message: 'Missing url query parameter' }, { status: 400 })
        }

        const backendRes = await fetch(`${BACKEND_INFO_URL}${encodeURIComponent(videoUrl)}`)
        const data = await backendRes.json()

        if (!backendRes.ok) {
            return NextResponse.json(data, { status: backendRes.status })
        }

        return NextResponse.json(data)
    } catch (error) {
        console.error('[YT Info] Error:', error)
        return NextResponse.json({ message: 'Failed to reach auth service' }, { status: 500 })
    }
}
