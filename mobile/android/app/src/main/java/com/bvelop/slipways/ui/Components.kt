package com.bvelop.slipways.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Button
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.LinkAnnotation
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextLinkStyles
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.withLink
import androidx.compose.ui.unit.dp
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.WaterLevel

/** The ODbL attribution for entries from OpenStreetMap, linked to the copyright page. */
@Composable
fun OsmAttribution(modifier: Modifier = Modifier) {
    LinkText(Links.OSM_ATTRIBUTION, Links.OSM_COPYRIGHT, modifier, MaterialTheme.typography.labelSmall)
}

@Composable
fun LinkText(
    text: String,
    url: String,
    modifier: Modifier = Modifier,
    style: androidx.compose.ui.text.TextStyle = MaterialTheme.typography.bodyLarge,
) {
    val linkStyle = TextLinkStyles(SpanStyle(color = MaterialTheme.colorScheme.primary))
    Text(
        buildAnnotatedString { withLink(LinkAnnotation.Url(url, linkStyle)) { append(text) } },
        modifier = modifier,
        style = style,
    )
}

@Composable
fun WaterLevelText(level: WaterLevel, modifier: Modifier = Modifier) {
    val color = when (level.state) {
        "low" -> Color(0xFFE69500)
        "high" -> MaterialTheme.colorScheme.error
        "normal" -> Color(0xFF2E8B57)
        else -> MaterialTheme.colorScheme.onSurfaceVariant
    }
    Text(Format.waterLevel(level), modifier = modifier, color = color, fontWeight = FontWeight.SemiBold)
}

/** Label and value; nothing when the value is missing. */
@Composable
fun Fact(label: String, icon: ImageVector, value: String?, url: String? = null) {
    if (value.isNullOrBlank()) return
    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        Icon(icon, contentDescription = null, Modifier.size(20.dp), tint = MaterialTheme.colorScheme.primary)
        Column {
            Text(label, style = MaterialTheme.typography.labelMedium, fontWeight = FontWeight.SemiBold)
            if (url != null) LinkText(value, url) else Text(value, style = MaterialTheme.typography.bodyLarge)
        }
    }
}

@Composable
fun Centered(content: @Composable () -> Unit) {
    Box(Modifier.fillMaxSize().padding(16.dp), contentAlignment = Alignment.Center) { content() }
}

@Composable
fun LoadFailed(message: String, onRetry: () -> Unit) {
    Centered {
        Column(horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text(message, color = MaterialTheme.colorScheme.error)
            Button(onClick = onRetry) { Text("Erneut versuchen") }
        }
    }
}
