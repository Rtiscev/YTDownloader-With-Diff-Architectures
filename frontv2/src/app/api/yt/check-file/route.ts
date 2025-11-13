import { NextRequest, NextResponse } from 'next/server'

export async function POST(req: NextRequest) {
    try {
        const { url, qualityLabel, mediaType, fileName } = await req.json()

        if (!url) {
            return NextResponse.json({ error: 'URL required' }, { status: 400 })
        }

        const backendRes = await fetch('http://apigateway:5000/api/ytdlp/check-file', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ url, qualityLabel, mediaType, fileName }),
        })

        if (!backendRes.ok) {
            return NextResponse.json(
                { error: 'Backend error' },
                { status: backendRes.status }
            )
        }

        const result = await backendRes.json()
        return NextResponse.json(result)
    } catch (error) {
        console.error('[Check File] Error:', error)
        return NextResponse.json(
            { error: 'Failed to check file', exists: false },
            { status: 500 }
        )
    }
}
