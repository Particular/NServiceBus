<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
  <xsl:output method="xml" indent="yes" encoding="utf-8"/>
  <xsl:param name="fileName" />

  <xsl:template name="AddReferences">
    <xsl:variable name="references" select="document($fileName)"/>
    <xsl:for-each select="$references/ItemGroup/*">
        <xsl:copy-of select="." />
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="@*|node()">
    <xsl:choose>
      <xsl:when test="local-name() = 'Reference' and ./@Include='NServiceBus'">
        <xsl:copy>
          <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
		<xsl:call-template name="AddReferences">
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:copy>
          <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
      </xsl:otherwise>
    </xsl:choose>
    
  </xsl:template>

</xsl:stylesheet>
